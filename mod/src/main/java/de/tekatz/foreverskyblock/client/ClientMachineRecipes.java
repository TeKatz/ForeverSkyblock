package de.tekatz.foreverskyblock.client;

/** JEI installs these callbacks only while its runtime exists; common screens load without JEI. */
public final class ClientMachineRecipes {
    private static Runnable condenser, washing, ascension;
    private ClientMachineRecipes() { }
    public static void install(Runnable c, Runnable w, Runnable a) { condenser=c; washing=w; ascension=a; }
    public static void clear() { condenser=null; washing=null; ascension=null; }
    public static boolean available() { return condenser!=null || washing!=null || ascension!=null; }
    public static void condenser() { if(condenser!=null)condenser.run(); }
    public static void washing() { if(washing!=null)washing.run(); }
    public static void ascension() { if(ascension!=null)ascension.run(); }
}
