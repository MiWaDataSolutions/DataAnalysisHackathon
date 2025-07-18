import { useGetDataSessionById, useStartGeneration } from "@/hooks/data-analyst-api/data-session.query";
import { useParams } from "react-router-dom"
import { z } from "zod"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Link } from "lucide-react";
import { Button } from "@/components/ui/button";
import * as tus from 'tus-js-client'
import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { Progress } from "@/components/ui/progress";
import { Input } from "@/components/ui/input";
import { toast } from "sonner"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { useSignalRWrapper } from "@/providers/signalr.provider";
import { useGetGraphData, useGetKPIData } from "@/hooks/data-analyst-api/graphing.query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PieChart, Pie, Cell, BarChart, Bar, CartesianGrid, XAxis } from 'recharts';
import { ChartContainer, type ChartConfig } from "@/components/ui/chart";


export const DOCUMENT_SCHEMA = z
  .instanceof(File)
  .refine(
    (file) =>
      [
        "text/csv"
      ].includes(file.type),
    { message: "Invalid document file type" }
  );

  
interface ErrorType {
  img_upload?: string;
  doc_upload?: string;
}

export const DataSession = () => {
    const [docFile, setDocFile] = useState<File>();
    const [error, setError] = useState<ErrorType>({});
    const [uploadInprogress, setUploadInprogress] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);
    const [dataSessionName, setDataSessionName] = useState<string>("Name Will Generate After First File is Uploaded");
    const { dataSessionId } = useParams();
    const { user } = useAuth();
    const { useSignalREffect } = useSignalRWrapper();
    const chartConfig = {
        desktop: {
            label: "Desktop",
            color: "#2563eb",
        },
        mobile: {
            label: "Mobile",
            color: "#60a5fa",
        },
    } satisfies ChartConfig

    const graphChartConfig = {
        xAxis: {
            label: "X Axis",
            color: "#2563eb",
        },
        yAxis: {
            label: "Y Axis",
            color: "#60a5fa",
        },
    } satisfies ChartConfig

    const COLORS = ['#0088FE', '#e0e0e0'];
    
    const { data: dataSessionData, error: dataSessionError } = useGetDataSessionById(dataSessionId!);
    if (dataSessionError) throw dataSessionError;

    const { data: graphKPIData, error: graphKPIDataError } = useGetKPIData(dataSessionData?.id, dataSessionData?.processedStatus);

    if (graphKPIDataError) throw graphKPIDataError;

    const { data: graphData, error: graphDataError } = useGetGraphData(dataSessionData?.id, dataSessionData?.processedStatus);

    if (graphDataError) throw graphDataError;

    useEffect(() => {
        setDataSessionName(dataSessionData?.name ?? "Name Will Generate After First File is Uploaded");
    }, [dataSessionData]);

    useSignalREffect("RecieveDataSessionName", (receivedDataSessionId, name) => {
        console.log(`receivedDataSessionId`, receivedDataSessionId);
        console.log(`receivedDataSessionId`, receivedDataSessionId);
        if (receivedDataSessionId == dataSessionId) {
            console.log(`hit`);
            setDataSessionName(name);
        }
    }, []);
    
    useSignalREffect("RecieveDataSessionDataGenerationComplete", (receivedDataSessionId) => {
        console.log(`RecieveDataSessionDataGenerationComplete`, receivedDataSessionId);
        if (receivedDataSessionId == dataSessionId) {
            console.log(`hit`);
            toast("AI Processing Steps Completed.", {
                description: `AI Processing completed in the background.`
            });
        }
    }, []);

    const { mutateAsync: startGenerationMuatateAsync } = useStartGeneration();

    const formSchema = z.object({
        generationOption: z.enum(['Select an Option', 'Dashboard']),
        file: z.instanceof(File),
        fileHasHeaders: z.enum(["Select an Option", "Yes", "No"])
    });
    const form = useForm<z.infer<typeof formSchema>>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            generationOption: "Select an Option",
            fileHasHeaders: "Select an Option"
        },
    })

    async function onSubmit(values: z.infer<typeof formSchema>) {
        if (values.generationOption == "Select an Option") {
            toast.error("Please select a Generation Option");
            return;
        }

        if (values.fileHasHeaders == "Select an Option") {
            toast.error("Please select an option for whether the file has headers");
            return;
        }
        
        if (!docFile) {
            setError({
                doc_upload: !docFile ? "Document file is required" : undefined
            });
            return;
        }

        const upload = new tus.Upload(docFile, {
            endpoint: `${import.meta.env.VITE_DATA_ANALYST_API_URL}/files/`,
            // Retry delays will enable tus-js-client to automatically retry on errors
            retryDelays: [0, 3000, 5000, 10000, 20000],
            // Attach additional meta data about the file for the server
            metadata: {
                filename: `${docFile.name}`,
                filetype: docFile.type,
                dataSessionId: dataSessionId!,
                userId: user?.googleId!
            },
            onBeforeRequest: function (req) {    
                setUploadInprogress(true);
                setUploadProgress(0);
            },
            // Callback for errors which cannot be fixed using retries
            onError: function (error) {
                console.log('Failed because: ' + error)
                setUploadInprogress(false);
                setUploadProgress(0);
            },
            // Callback for reporting upload progress
            onProgress: function (bytesUploaded, bytesTotal) {
                var percentage = ((bytesUploaded / bytesTotal) * 100).toFixed(2)
                setUploadProgress(+percentage);
            },
            // Callback for once the upload is completed
            onSuccess: function () {
                toast("File Upload Complete", {
                    description: "AI Preperation Steps started"
                });
                setUploadInprogress(false);
                setUploadProgress(0);
                setDocFile(undefined);
                startGenerationMuatateAsync({
                    dataSessionId: dataSessionId!,
                    filename: docFile.name,
                    initialFileHasHeaders: values.fileHasHeaders == "Yes"
                }).then(() => {
                    toast("AI Preperation Steps Completed.", {
                        description: `AI Processing in the background. You will be notified once your ${values.generationOption} has been generated.`
                    });
                });
            },
        });

        upload.findPreviousUploads().then(function (previousUploads) {
            // Found previous uploads so we select the first one.
            if (previousUploads.length) {
                upload.resumeFromPreviousUpload(previousUploads[0])
            }

            // Start the upload
            upload.start()
        });
    }
    const validateFile = (file: File, schema: any, field: keyof ErrorType) => {
        const result = schema.safeParse(file);
        if (!result.success) {
        setError((prevError) => ({
            ...prevError,
            [field]: result.error.errors[0].message,
        }));
        return false;
        } else {
        setError((prevError) => ({
            ...prevError,
            [field]: undefined,
        }));
        return true;
        }
    };

    const handleDocChange = (e: React.ChangeEvent<HTMLInputElement>, onChange: (...event: any[]) => void) => {
        const file = e.target.files?.[0];
        if (file) {
            const isValid = validateFile(file, DOCUMENT_SCHEMA, "doc_upload");
            if (isValid) {
                setDocFile(file);
                onChange(file);
            }
        }
    };

    return (
        <div className="flex flex-col gap-2 place-content-center m-x-2">
            <h1>{dataSessionName}</h1>
            {uploadInprogress &&
            <Progress value={uploadProgress} />
            }
            {dataSessionName == "Name Will Generate After First File is Uploaded" && <div className="w-lg">
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8 flex flex-col justify-center w-lg">
                        <FormField
                            control={form.control}
                            name="generationOption"
                            render={({ field }) => (
                                <FormItem className="w-lg">
                                    <FormLabel>Generation Option</FormLabel>
                                    <Select onValueChange={field.onChange} defaultValue={field.value}>
                                        <FormControl className="w-lg">
                                            <SelectTrigger className="w-lg">
                                                <SelectValue placeholder="Select a Generation Option" />
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent className="w-lg">
                                            <SelectGroup>
                                                <SelectLabel>Generation Option</SelectLabel>
                                                <SelectItem value="Dashboard">Dashboard</SelectItem>
                                            </SelectGroup>
                                        </SelectContent>
                                    </Select>
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="file"
                            render={({ field }) => (
                                <>
                                <FormItem className="w-lg">
                                    <FormLabel>Select File To Upload</FormLabel>
                                    <FormControl>
                                        <Input className="w-lg" type="file" onChange={(e) => {
                                            handleDocChange(e, field.onChange)
                                        }}  />
                                    </FormControl>
                                </FormItem>
                                {error.doc_upload && <p className="error">{error.doc_upload}</p>}
                                </>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="fileHasHeaders"
                            render={({ field }) => (
                                <FormItem className="w-lg">
                                    <FormLabel>File Has Headers?</FormLabel>
                                    <FormControl>
                                        <RadioGroup
                                            onValueChange={field.onChange}
                                            defaultValue={field.value}
                                            className="flex flex-row"
                                        >
                                            <FormItem className="flex items-center gap-3">
                                                <FormControl>
                                                    <RadioGroupItem value="Select an Option" />
                                                </FormControl>
                                                <FormLabel className="font-normal">
                                                    Select an Option
                                                </FormLabel>
                                            </FormItem>
                                            <FormItem className="flex items-center gap-3">
                                                <FormControl>
                                                    <RadioGroupItem value="Yes" />
                                                </FormControl>
                                                <FormLabel className="font-normal">
                                                    Yes
                                                </FormLabel>
                                            </FormItem>
                                            <FormItem className="flex items-center gap-3">
                                                <FormControl>
                                                    <RadioGroupItem value="No" />
                                                </FormControl>
                                                <FormLabel className="font-normal">No</FormLabel>
                                            </FormItem>
                                        </RadioGroup>
                                    </FormControl>
                                </FormItem>
                            )}
                        />
                        <Button type="submit">Submit</Button>
                    </form>
                </Form>
            </div>
            }
            {dataSessionData?.processedStatus == 1 && <div>
                <h1>Data Processing...</h1>
            </div>}
            <div className="flex flex-col justify-between">
                <div className="flex flex-row justify-between">
            {dataSessionData?.processedStatus == 2 && graphKPIData && graphKPIData.map((kpi) => (
                <Card key={kpi.kpiName}>
                    <CardHeader>
                        <CardTitle>{kpi.kpiName} - Last 30 Days</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <ChartContainer config={chartConfig} className="min-h-[200px] w-full">
                            <PieChart width={200} height={120}>
                                <Pie
                                    data={[
                                        {
                                            value: kpi.last30Days! > 6000 ? 1 : kpi.last30Days! / 6000,
                                        },
                                        {
                                            value: 1 - (kpi.last30Days! > 6000 ? 1 : kpi.last30Days! / 6000)
                                        }
                                    ]}
                                    startAngle={180}
                                    endAngle={0}
                                    innerRadius={60}
                                    outerRadius={80}
                                    paddingAngle={5}
                                    dataKey="value"
                                >
                                    {[
                                        {
                                            value: kpi.last30Days! > 6000 ? 1 : kpi.last30Days! / 6000,
                                        },
                                        {
                                            value: 1 - (kpi.last30Days! > 6000 ? 1 : kpi.last30Days! / 6000)
                                        }
                                    ].map((entry, index) => (
                                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                    ))}
                                </Pie>
                                <text
                                    x={100}
                                    y={100}
                                    textAnchor="middle"
                                    dominantBaseline="middle"
                                    fontSize="24"
                                >
                                    {kpi.last30Days!}
                                </text>
                            </PieChart>
                        </ChartContainer>
                    </CardContent>
                </Card>
            ))} 
            </div>
            <div className="flex flex-row justify-between">
            {dataSessionData?.processedStatus == 2 && graphData && Object.keys(graphData).map((graph) => (
                    <Card key={graph}>
                        <CardHeader>
                            <CardTitle>{graph}</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <ChartContainer config={graphChartConfig} className="min-h-[200px] w-full">
                                <BarChart accessibilityLayer data={graphData[graph].map((data) => {
                                    return { xAxis: data.xAxis, yAxis: data.yAxis }
                                })}>
                                    <CartesianGrid vertical={false} />
                                    <XAxis
                                        dataKey="xAxis"
                                        tickLine={false}
                                        tickMargin={10}
                                        axisLine={false}
                                        tickFormatter={(value) => value.slice(0, 3)}
                                    />
                                    <Bar dataKey="yAxis" fill="var(--color-yAxis)" radius={4} />
                                </BarChart>
                            </ChartContainer>
                        </CardContent>
                    </Card>
            ))}
            </div>

            </div>
        </div>
    )
}