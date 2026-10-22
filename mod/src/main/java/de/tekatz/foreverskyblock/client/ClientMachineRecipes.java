package de.tekatz.foreverskyblock.client;

/** JEI installs these callbacks only while its runtime exists; common screens load without JEI. */
public final class ClientMachineRecipes {
    private static Runnable condenser, washing, garden, ascension, crucible, refining;
    private ClientMachineRecipes() { }
    public static void install(Runnable c, Runnable w, Runnable g, Runnable a, Runnable cr, Runnable r) {
        condenser=c; washing=w; garden=g; ascension=a; crucible=cr; refining=r;
    }
    public static void clear() { condenser=null; washing=null; garden=null; ascension=null; crucible=null; refining=null; }
    public static boolean available() { return condenser!=null || washing!=null || garden!=null || ascension!=null || crucible!=null || refining!=null; }
    public static void condenser() { if(condenser!=null)condenser.run(); }
    public static void washing() { if(washing!=null)washing.run(); }
    public static void garden() { if(garden!=null)garden.run(); }
    public static void ascension() { if(ascension!=null)ascension.run(); }
    public static void crucible() { if(crucible!=null)crucible.run(); }
    public static void refining() { if(refining!=null)refining.run(); }
}
