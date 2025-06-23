using Pulumi;
using Pulumi.Gcp.ArtifactRegistry;
using Pulumi.Gcp.ArtifactRegistry.Inputs;
using Pulumi.Gcp.CloudRun;
using Pulumi.Gcp.CloudRun.Inputs;
using Pulumi.Gcp.Compute;
using Pulumi.Gcp.Compute.Inputs;
using Pulumi.Gcp.DataCatalog;
using Pulumi.Gcp.ManagedKafka;
using Pulumi.Gcp.ServiceNetworking;
using Pulumi.Gcp.Sql;
using Pulumi.Gcp.Sql.Inputs;
using Pulumi.Gcp.Storage;
using Pulumi.Gcp.VpcAccess;
using System;
using System.Collections.Generic;
using System.Linq;

return await Deployment.RunAsync(() =>
{
    var config = new Pulumi.Config();
    var databasePassword = config.GetSecret("DATABASE_PASSWORD");
    var rabbitMQUser = config.Get("RABBITMQ_USER");
    var rabbitMQPassword = config.GetSecret("RABBITMQ_PASSWORD");
    var googleClientSecret = config.GetSecret("GoogleClientSecret");

    var location = "europe-west1";
    const string projectNamespace = "data-analysis-hackathon";

    // Create a network for the VM
    var insightfulNetwork = new Network("insightful-network", new NetworkArgs
    {
        AutoCreateSubnetworks = true,
    });

    // Create a firewall rule to allow RabbitMQ (5672) and management (15672) ports
    var firewall = new Firewall("insightful-firewall", new FirewallArgs
    {
        Network = insightfulNetwork.Id,
        Allows =
            {
                new FirewallAllowArgs
                {
                    Protocol = "tcp",
                    Ports = { "22", "5672", "15672", "443" }, // SSH, RabbitMQ, RabbitMQ Management
                },
            },
        SourceRanges = { "0.0.0.0/0" }, // Open to all for demo; restrict in production
        Description = "Allow SSH, RabbitMQ Management UI, Frontend",
    });

    var image = Output.Create(GetImage.InvokeAsync(new GetImageArgs
    {
        Family = "ubuntu-minimal-2504-amd64",
        Project = "ubuntu-os-cloud"
    }));
    // Startup script to install RabbitMQ
    var startupScript = @"#!/bin/bash
sudo apt-get update -y
sudo apt-get install curl gnupg -y
sudo apt-get install apt-transport-https
## Team RabbitMQ's main signing key
curl -1sLf ""https://keys.openpgp.org/vks/v1/by-fingerprint/0A9AF2115F4687BD29803A206B73A36E6026DFCA"" | sudo gpg --dearmor | sudo tee /usr/share/keyrings/com.rabbitmq.team.gpg > /dev/null
## Community mirror of Cloudsmith: modern Erlang repository
curl -1sLf https://github.com/rabbitmq/signing-keys/releases/download/3.0/cloudsmith.rabbitmq-erlang.E495BB49CC4BBE5B.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/rabbitmq.E495BB49CC4BBE5B.gpg > /dev/null
## Community mirror of Cloudsmith: RabbitMQ repository
curl -1sLf https://github.com/rabbitmq/signing-keys/releases/download/3.0/cloudsmith.rabbitmq-server.9F4587F226208342.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/rabbitmq.9F4587F226208342.gpg > /dev/null
sudo tee /etc/apt/sources.list.d/rabbitmq.list <<EOF
## Provides modern Erlang/OTP releases from a Cloudsmith mirror
##
deb [arch=amd64 signed-by=/usr/share/keyrings/rabbitmq.E495BB49CC4BBE5B.gpg] https://ppa1.rabbitmq.com/rabbitmq/rabbitmq-erlang/deb/ubuntu noble main
deb-src [signed-by=/usr/share/keyrings/rabbitmq.E495BB49CC4BBE5B.gpg] https://ppa1.rabbitmq.com/rabbitmq/rabbitmq-erlang/deb/ubuntu noble main

# another mirror for redundancy
deb [arch=amd64 signed-by=/usr/share/keyrings/rabbitmq.E495BB49CC4BBE5B.gpg] https://ppa2.rabbitmq.com/rabbitmq/rabbitmq-erlang/deb/ubuntu noble main
deb-src [signed-by=/usr/share/keyrings/rabbitmq.E495BB49CC4BBE5B.gpg] https://ppa2.rabbitmq.com/rabbitmq/rabbitmq-erlang/deb/ubuntu noble main

## Provides RabbitMQ from a Cloudsmith mirror
##
deb [arch=amd64 signed-by=/usr/share/keyrings/rabbitmq.9F4587F226208342.gpg] https://ppa1.rabbitmq.com/rabbitmq/rabbitmq-server/deb/ubuntu noble main
deb-src [signed-by=/usr/share/keyrings/rabbitmq.9F4587F226208342.gpg] https://ppa1.rabbitmq.com/rabbitmq/rabbitmq-server/deb/ubuntu noble main

# another mirror for redundancy
deb [arch=amd64 signed-by=/usr/share/keyrings/rabbitmq.9F4587F226208342.gpg] https://ppa2.rabbitmq.com/rabbitmq/rabbitmq-server/deb/ubuntu noble main
deb-src [signed-by=/usr/share/keyrings/rabbitmq.9F4587F226208342.gpg] https://ppa2.rabbitmq.com/rabbitmq/rabbitmq-server/deb/ubuntu noble main
EOF
sudo apt-get update -y

## Install Erlang packages
sudo apt-get install -y erlang-base \
                        erlang-asn1 erlang-crypto erlang-eldap erlang-ftp erlang-inets \
                        erlang-mnesia erlang-os-mon erlang-parsetools erlang-public-key \
                        erlang-runtime-tools erlang-snmp erlang-ssl \
                        erlang-syntax-tools erlang-tftp erlang-tools erlang-xmerl

## Install rabbitmq-server and its dependencies
sudo apt-get install rabbitmq-server -y --fix-missing
sudo systemctl enable rabbitmq-server
sudo systemctl start rabbitmq-server
sudo rabbitmq-plugins enable rabbitmq_management
sudo systemctl restart rabbitmq-server
sudo rabbitmqctl add_user pulumi pulumi123
sudo rabbitmqctl set_user_tags pulumi administrator
sudo rabbitmqctl set_permissions -p / pulumi "".*"" "".*"" "".*""
";

    // Create the VM instance
    var insightfulRabbitMQInstance = new Instance("rabbitmq-vm", new InstanceArgs
    {
        MachineType = "e2-medium",
        Zone = $"{location}-b",
        BootDisk = new InstanceBootDiskArgs
        {
            InitializeParams = new InstanceBootDiskInitializeParamsArgs
            {
                Image = image.Apply(i => i.SelfLink),
            },
        },
        NetworkInterfaces =
            {
                new InstanceNetworkInterfaceArgs
                {
                    Network = insightfulNetwork.Id,
                    AccessConfigs = { new InstanceNetworkInterfaceAccessConfigArgs {} }, // Assign external IP
                },
            },
        MetadataStartupScript = startupScript,
        Tags = { "rabbitmq" },
    });    // Export the public IP and connection string
    var InstancePublicIp = insightfulRabbitMQInstance.NetworkInterfaces.Apply(nics => nics[0].AccessConfigs[0].NatIp);
    var RabbitmqConnectionString = Output.Format($"amqp://pulumi:pulumi123@{InstancePublicIp}:5672/");

    var postgresPrivateIpAddress = new GlobalAddress("postgres-private-ip-address", new()
    {
        Name = "postgres-private-ip-address",
        Purpose = "VPC_PEERING",
        AddressType = "INTERNAL",
        PrefixLength = 16,
        Network = insightfulNetwork.Id,
    });

    var privateVpcConnection = new Connection("private_vpc_connection", new()
    {
        Network = insightfulNetwork.Id,
        Service = "servicenetworking.googleapis.com",
        ReservedPeeringRanges = 
        {
            postgresPrivateIpAddress.Name,
        },
    });

    var vpcConnector = new Pulumi.Gcp.VpcAccess.Connector("vpc-connector", new Pulumi.Gcp.VpcAccess.ConnectorArgs
    {
        Name = "insightful-vpc-connector",
        Region = location,
        IpCidrRange = "10.8.0.0/28", // Choose a range that doesn't conflict
        Network = insightfulNetwork.Name,
        MinThroughput = 200,
        MaxThroughput = 300
    }, new CustomResourceOptions
    {
        DependsOn = { privateVpcConnection }
    });

    var postgresInstance = new DatabaseInstance("postgres", new()
    {
        Name = "insightful-instance",
        Region = location,
        DatabaseVersion = "POSTGRES_15",
        Settings = new DatabaseInstanceSettingsArgs
        {
            Tier = "db-f1-micro",
            IpConfiguration = new DatabaseInstanceSettingsIpConfigurationArgs
            {
                Ipv4Enabled = false,
                PrivateNetwork = insightfulNetwork.SelfLink,
                EnablePrivatePathForGoogleCloudServices = true,
            },
        },
        DeletionProtection = false,
    }, new CustomResourceOptions
    {
        DependsOn =
        {
            privateVpcConnection,
        },
    });

    var user = new User("user", new()
    {
        Name = "insightful-application",
        Instance = postgresInstance.Name,
        Password = databasePassword,
    });

    var mainPostgresDatabase = new Database("insightful-db", new()
    {
        Name = "insightful-db",
        Instance = postgresInstance.Name
    });

    var postgresPrivateIp = postgresInstance.IpAddresses.Apply(ips => ips.FirstOrDefault(ip => ip.Type == "PRIVATE")?.IpAddress ?? ""
);
    var connectionString = Output.Format($"Host={postgresPrivateIp};Username={user.Name};Password={databasePassword};Database={mainPostgresDatabase.Name}");
    var defaultUserConnectionString = Output.Format($"Host={postgresPrivateIp};Username={user.Name};Password={databasePassword};Database={mainPostgresDatabase.Name}");

    var insightfulArtifactRegistry = new Repository("insightful-artifactregistry", new()
    {
        Location = location,
        RepositoryId = "insightful-artifactregistry",
        Description = "Insightful Artifact Registry",
        Format = "DOCKER",
        DockerConfig = new RepositoryDockerConfigArgs
        {
            ImmutableTags = false,
        },
    });    string frontendImageName = "insightful-frontend";
    string backendImageName = "insightful-backend";
    string agentImageName = "insightful-agent";

    var frontendImageUri = Output.Format($"{location}-docker.pkg.dev/{insightfulArtifactRegistry.Project}/{insightfulArtifactRegistry.RepositoryId}/{frontendImageName}:latest");
    var backendImageUri = Output.Format($"{location}-docker.pkg.dev/{insightfulArtifactRegistry.Project}/{insightfulArtifactRegistry.RepositoryId}/{backendImageName}:latest");
    var agentImageUri = Output.Format($"{location}-docker.pkg.dev/{insightfulArtifactRegistry.Project}/{insightfulArtifactRegistry.RepositoryId}/{agentImageName}:latest");

    var frontendImage = new Pulumi.Docker.Image("frontend-image", new Pulumi.Docker.ImageArgs
    {
        Build = new Pulumi.Docker.Inputs.DockerBuildArgs { Context = "../frontend" }, // Path to your Dockerfile
        ImageName = frontendImageUri,
        Registry = new Pulumi.Docker.Inputs.RegistryArgs
        {
            Server = $"{location}-docker.pkg.dev"
        }
    });

    var backendImage = new Pulumi.Docker.Image("backend-image", new Pulumi.Docker.ImageArgs
    {
        Build = new Pulumi.Docker.Inputs.DockerBuildArgs { Dockerfile = "../backend/DataAnalystBackend/Dockerfile", Context = "../backend" }, // Path to your Dockerfile
        ImageName = backendImageUri,
        Registry = new Pulumi.Docker.Inputs.RegistryArgs
        {
            Server = $"{location}-docker.pkg.dev"
        }
    });

    var agentImage = new Pulumi.Docker.Image("agent-image", new Pulumi.Docker.ImageArgs
    {
        Build = new Pulumi.Docker.Inputs.DockerBuildArgs { Context = "../agent" }, // Path to your Dockerfile
        ImageName = agentImageUri,
        Registry = new Pulumi.Docker.Inputs.RegistryArgs
        {
            Server = $"{location}-docker.pkg.dev"
        }
    });   
    
    var frontendRun = new Service("frontend-run", new()
    {
        Name = "frontend-run-srv",
        Location = location,
        Metadata = new ServiceMetadataArgs
        {
            Namespace = projectNamespace,
        },
        Template = new ServiceTemplateArgs
        {
            Spec = new ServiceTemplateSpecArgs
            {
                Containers = new[]
                {
                    new ServiceTemplateSpecContainerArgs
                    {
                        Image = frontendImageUri,
                        Ports = new[]
                        {
                            new ServiceTemplateSpecContainerPortArgs
                            {
                                ContainerPort = 8080,
                            }
                        },
                    },
                },
                
            },
            Metadata = new ServiceTemplateMetadataArgs()
            {
                Annotations =
                {
                    // Attach the VPC connector using annotation
                    { "run.googleapis.com/vpc-access-connector", vpcConnector.Name }
                }
            }
        },
        Traffics = new ServiceTrafficArgs
        {
            Percent = 100,
            LatestRevision = true
        }
    }, new CustomResourceOptions
    {
        DependsOn = { frontendImage }
    });    
    
    var frontendDomainMapping = new DomainMapping("frontend-domain-mapping", new()
    {
        Location = location,
        Name = "insightful.michaelrademeyer.dev",
        Metadata = new DomainMappingMetadataArgs
        {
            Namespace = projectNamespace,
        },
        Spec = new DomainMappingSpecArgs
        {
            RouteName = frontendRun.Name,
        }
    }, new CustomResourceOptions
    {
        DependsOn = { frontendRun }
    }); ;   
    
    var backendRun = new Service("backend-run", new()
    {
        Name = "backend-run-srv",
        Location = location,
        Metadata = new ServiceMetadataArgs
        {
            Namespace = projectNamespace,
        },
        Template = new ServiceTemplateArgs
        {
            Spec = new ServiceTemplateSpecArgs
            {
                Containers = new[]
                {
                    new ServiceTemplateSpecContainerArgs
                    {
                        Image = backendImageUri,
                        Ports = new[]
                        {
                            new ServiceTemplateSpecContainerPortArgs
                            {
                                ContainerPort = 8080
                            }
                        },
                        Envs = new InputList<ServiceTemplateSpecContainerEnvArgs>()
                        {
                            new ServiceTemplateSpecContainerEnvArgs()
                            {
                                Name = "ConnectionStrings__DefaultConnection",
                                Value = connectionString
                            },
                            new ServiceTemplateSpecContainerEnvArgs()
                            {
                                Name = "DefaultUserDatabaseConnection",
                                Value = defaultUserConnectionString
                            },
                            new ServiceTemplateSpecContainerEnvArgs()
                            {
                                Name = "Google__ClientSecret",
                                Value = googleClientSecret
                            },
                            new ServiceTemplateSpecContainerEnvArgs()
                            {
                                Name = "RabbitMQ__HostName",
                                Value = RabbitmqConnectionString
                            },
                            new ServiceTemplateSpecContainerEnvArgs()
                            {
                                Name = "RabbitMQ__Prefix",
                                Value = "prod"
                            }
                        }

                    },
                },
            },
            Metadata = new ServiceTemplateMetadataArgs()
            {
                Annotations =
                {
                    // Attach the VPC connector using annotation
                    { "run.googleapis.com/vpc-access-connector", vpcConnector.Name }
                }
            }
        },
        Traffics = new ServiceTrafficArgs
        {
            Percent = 100,
            LatestRevision = true
        }
    }, new CustomResourceOptions
    {
        DependsOn = { backendImage }
    });    
    
    var backendDomainMapping = new DomainMapping("backend-domain-mapping", new()
    {
        Location = location,
        Name = "insightfulapi.michaelrademeyer.dev",        
        Metadata = new DomainMappingMetadataArgs
        {
            Namespace = projectNamespace,
        },
        Spec = new DomainMappingSpecArgs
        {
            RouteName = backendRun.Name,
        },
    }, new CustomResourceOptions
    {
        DependsOn = { backendRun }
    });

    var agentRun = new Pulumi.Gcp.CloudRunV2.Job("agent-run", new()
    {
        Name = "agent-run-srv",
        Location = location,
        DeletionProtection = false,
        Template = new Pulumi.Gcp.CloudRunV2.Inputs.JobTemplateArgs
        {
            Template = new Pulumi.Gcp.CloudRunV2.Inputs.JobTemplateTemplateArgs
            {
                Containers = new[]
                {
                    new Pulumi.Gcp.CloudRunV2.Inputs.JobTemplateTemplateContainerArgs
                    {
                        Image = agentImageUri,
                    },
                },
                VpcAccess = new Pulumi.Gcp.CloudRunV2.Inputs.JobTemplateTemplateVpcAccessArgs
                {
                    Connector = vpcConnector.Id,
                    Egress = "ALL_TRAFFIC",
                }
            },
        },
    }, new CustomResourceOptions
    {
        DependsOn = { agentImage, vpcConnector }
    });

    // Export the DNS name of the bucket
    return new Dictionary<string, object?>
    {
        ["frontendUrl"] = frontendDomainMapping.Name,
    };
});
