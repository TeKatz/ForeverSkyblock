package de.tekatz.foreverskyblock.client;

/** JEI installs these callbacks only while its runtime exists; common screens load without JEI. */
public final class ClientMachineRecipes {
    private static Runnable condenser,ascension;
    private ClientMachineRecipes() { }
    public static void install(Runnable c,Runnable a) { condenser=c;ascension=a; }
    public static void clear() { condenser=null;ascension=null; }
    public static boolean available() { return condenser!=null; }
    public static void condenser() { if(condenser!=null)condenser.run(); }
    public static void ascension() { if(ascension!=null)ascension.run(); }
}
