package de.tekatz.foreverskyblock.menu;

/** Common logical coordinates keep rendered slots, server menus and pointer transforms aligned. */
public record MachineLayout(int width, int height, int inventoryY, int hotbarY) {
    public static final MachineLayout CONDENSER = new MachineLayout(248,296,204,268);
    public static final MachineLayout ASCENSION = new MachineLayout(248,270,176,240);
    public int inventoryX(int column) { return 24 + column*22; }
    public int inventoryRowY(int row) { return inventoryY + row*20; }
    public Viewport viewport(int windowWidth,int windowHeight,int logicalLeft,int logicalTop) {
        double scale=Math.min(1,Math.min((windowWidth-12.0)/width,(windowHeight-12.0)/height));
        scale=Math.max(.1,scale);
        return new Viewport(scale,(windowWidth-width*scale)/2,(windowHeight-height*scale)/2,logicalLeft,logicalTop);
    }
    public record Viewport(double scale,double x,double y,int logicalLeft,int logicalTop) {
        public double logicalX(double screenX) { return (screenX-x)/scale+logicalLeft; }
        public double logicalY(double screenY) { return (screenY-y)/scale+logicalTop; }
        public double screenX(double logicalX) { return x+(logicalX-logicalLeft)*scale; }
        public double screenY(double logicalY) { return y+(logicalY-logicalTop)*scale; }
    }
}
