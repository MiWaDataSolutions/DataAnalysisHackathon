import { Button } from '@/components/ui/button';
import { Drawer, DrawerClose, DrawerContent, DrawerDescription, DrawerFooter, DrawerHeader, DrawerTitle, DrawerTrigger } from '@/components/ui/drawer';
import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { useMediaQuery } from 'usehooks-ts'

interface DrawerContextType {
  isDesktop: boolean;
  drawerIsOpen: boolean;
  setDrawerOpen: (value:boolean) => void
}

const DrawerContext = createContext<DrawerContextType | undefined>(undefined);

export const DrawerProvider = ({ children }: { children: ReactNode }) => {
  const isDesktop = useMediaQuery("(min-width: 768px)");
  const [open, setOpen] = React.useState(false)

  return (
    <DrawerContext.Provider value={{isDesktop, drawerIsOpen: open, setDrawerOpen: setOpen }}>
      {children}
    </DrawerContext.Provider>
  )
}

export const useDrawer = () => {
  const context = useContext(DrawerContext);
  if (context === undefined) {
    throw new Error('useDrawer must be used within an DrawerProvider');
  }
  return context;
}