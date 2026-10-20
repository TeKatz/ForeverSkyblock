package de.tekatz.foreverskyblock.menu;

/** Common logical coordinates keep rendered slots, server menus and pointer transforms aligned. */
public record MachineLayout(int width, int height, int inventoryY, int hotbarY) {
    // MK II uses the extra vertical room for its automation/upgrade panel. Keeping the same layout for MK I
    // avoids a second menu protocol and leaves a compact "MK II only" explanation instead of dead controls.
    public static final MachineLayout CONDENSER = new MachineLayout(248,352,260,324);
    public static final MachineLayout WASHING = new MachineLayout(248,286,190,254);
    public static final MachineLayout ASCENSION = new MachineLayout(248,270,176,240);
    public int inventoryX(int column) { return 24 + column*22; }
    public int inventoryRowY(int row) { return inventoryY + row*20; }
    public Viewport viewport(int windowWidth,int windowHeight,int logicalLeft,int logicalTop) {
        int availableWidth=Math.max(1,windowWidth),availableHeight=Math.max(1,windowHeight);
        // Preserve the native pixel scale whenever the complete logical GUI already fits. The safety margin is
        // only needed when we genuinely have to shrink a machine screen; applying it unconditionally made the
        // 352 px Condenser/Garden layout render slightly below 100% at GUI scale 3.
        double scale=1.0;
        if(availableWidth<width||availableHeight<height) {
            scale=Math.min((availableWidth-12.0)/width,(availableHeight-12.0)/height);
        }
        scale=Math.max(.1,Math.min(1.0,scale));
        return new Viewport(scale,(windowWidth-width*scale)/2,(windowHeight-height*scale)/2,logicalLeft,logicalTop);
    }
    public record Viewport(double scale,double x,double y,int logicalLeft,int logicalTop) {
        public double logicalX(double screenX) { return (screenX-x)/scale+logicalLeft; }
        public double logicalY(double screenY) { return (screenY-y)/scale+logicalTop; }
        public double screenX(double logicalX) { return x+(logicalX-logicalLeft)*scale; }
        public double screenY(double logicalY) { return y+(logicalY-logicalTop)*scale; }
    }
}
