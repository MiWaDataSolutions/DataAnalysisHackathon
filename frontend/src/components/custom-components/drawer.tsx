import { Button } from '@/components/ui/button';
import { Drawer, DrawerClose, DrawerContent, DrawerDescription, DrawerFooter, DrawerHeader, DrawerTitle, DrawerTrigger } from '@/components/ui/drawer';
import { useAuth } from '@/context/AuthContext';
import { useCreateDataSession, useDeleteDataSession, useGetDataSessions } from '@/hooks/data-analyst-api/data-session.query';
import { useDrawer } from '@/providers/drawer-provider';
import { ArrowRight, Menu, Plus, SidebarClose, Trash, X } from "lucide-react";
import { useNavigate } from 'react-router-dom';

export const CustomDrawer = () => {
    const { drawerIsOpen, setDrawerOpen } = useDrawer();
    const { isAuthenticated, isLoading: authIsLoading, user } = useAuth();
    const { data: dataSessionData, error: dataSessionError } = useGetDataSessions(isAuthenticated);
    const navigate = useNavigate();

    if (dataSessionError) {
        throw dataSessionError;
    }

    const { mutateAsync: createDataSessionAsync } = useCreateDataSession();
    const { mutateAsync: deleteDataSessionAsync } = useDeleteDataSession();

    const handleCreateDataSession = async () => {
        const response = await createDataSessionAsync({
            user: {
                googleId: user?.googleId!,
                email: user?.email!,
                name: user?.name!
            },
            userId: user?.googleId!
        });
        
        const url = new URL(`/data-sessions/${response}`, window.location.origin);
        navigate(url.pathname);
    }

    const handleDeleteDataSessionAsync = async (dataSessionId: string) => {
        await deleteDataSessionAsync(dataSessionId);
        if (location.pathname.includes(dataSessionId)) {
            const url = new URL(`/`, window.location.origin);
            navigate(url.pathname);
        }
    }

    const handleNaviagteToDataSession = (dataSessionId: string) => {
        const url = new URL(`/data-sessions/${dataSessionId}`, window.location.origin);
        navigate(url.pathname);
    }

    if (authIsLoading) {
        return (
            <p>Loading Auth...</p>
        )
    }
    
    return (
        <Drawer open={drawerIsOpen} onOpenChange={setDrawerOpen}>
        <DrawerTrigger asChild>
          <Button variant="outline">
            <Menu />
          </Button>
        </DrawerTrigger>
        <DrawerContent>
          <DrawerHeader className="text-left">
            <DrawerTitle className='flex justify-center items-center gap-2'>
                Data Sessions
                <Button onClick={handleCreateDataSession}>
                    <Plus />
                </Button>
            </DrawerTitle>
          </DrawerHeader>
          {isAuthenticated && 
          <div className='flex justify-center gap-2'>
            <div className='flex justify-center flex-col gap-2'>
                {dataSessionData?.map((dataSession) => (
                    <div key={dataSession.id} className='border-2 border-dashed p-2'>
                        {dataSession.name ?? 'Name Will Generate After First File is Uploaded'} <Button className='bg-white hover:bg-red-500 hover:text-black' onClick={() => handleDeleteDataSessionAsync(dataSession.id!)}><Trash className='text-red-500' /></Button><Button onClick={() => handleNaviagteToDataSession(dataSession.id!)}><ArrowRight /></Button>
                    </div>
                ))}
            </div>
          </div>
          }
          <DrawerFooter className="pt-2">
            <DrawerClose asChild>
              <Button variant="outline"><X /></Button>
            </DrawerClose>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>
    )
}