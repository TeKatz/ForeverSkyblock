package de.tekatz.foreverskyblock.editor;

import java.io.*;
import java.nio.ByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.zip.*;

/** Atomic document replacement, explicit backup recovery and a fixed-path repository export format. */
public final class EditorStore {
    public static final String REPO_PATH="pack/config/forever_skyblock/kmenu.json";
    private final Path file;
    public EditorStore(Path file){this.file=file;}
    public boolean exists(){return Files.exists(file);}
    public EditorDocument load() throws IOException {
        if(!Files.exists(file)){if(Files.exists(file.resolveSibling(file.getFileName()+".bak")))throw new IOException("Primary K-menu config missing; restore the backup");return EditorDefaults.create();}
        return read(file);
    }
    public static EditorDocument read(Path path) throws IOException {
        if(Files.size(path)>EditorDocument.MAX_BYTES)throw new IOException("Document exceeds size limit");
        try{return EditorCodec.decode(Files.readString(path));}catch(RuntimeException e){throw new IOException("Invalid editor file: "+path,e);}
    }
    public void save(EditorDocument next) throws IOException {
        String data=EditorCodec.encode(next);
        if(Files.exists(file))atomic(file.resolveSibling(file.getFileName()+".bak"),Files.readString(file));
        atomic(file,data);
    }
    public EditorDocument readBackup() throws IOException {return read(file.resolveSibling(file.getFileName()+".bak"));}
    public static void atomic(Path file,String data) throws IOException {
        Path absolute=file.toAbsolutePath();Files.createDirectories(absolute.getParent());Path temp=Files.createTempFile(absolute.getParent(),"kmenu-",".tmp");
        try {
            try(var channel=FileChannel.open(temp,StandardOpenOption.WRITE)){ByteBuffer bytes=StandardCharsets.UTF_8.encode(data);while(bytes.hasRemaining())channel.write(bytes);channel.force(true);}
            try{Files.move(temp,absolute,StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING);}
            catch(AtomicMoveNotSupportedException e){Files.move(temp,absolute,StandardCopyOption.REPLACE_EXISTING);}
        }finally{Files.deleteIfExists(temp);}
    }
    public static void exportZip(EditorDocument document,Path path) throws IOException {
        byte[] data=EditorCodec.encode(document).getBytes(StandardCharsets.UTF_8);Files.createDirectories(path.toAbsolutePath().getParent());
        Path temp=Files.createTempFile(path.toAbsolutePath().getParent(),"kmenu-export-",".zip");
        try {
            try(var zip=new ZipOutputStream(Files.newOutputStream(temp))) {
                zip.putNextEntry(new ZipEntry(REPO_PATH));zip.write(data);zip.closeEntry();
                zip.putNextEntry(new ZipEntry("kmenu-export-manifest.json"));zip.write(("{\"schema\":1,\"revision\":"+document.revision()+",\"sha256\":\""+hash(data)+"\",\"path\":\""+REPO_PATH+"\"}").getBytes(StandardCharsets.UTF_8));zip.closeEntry();
            }
            Files.move(temp,path.toAbsolutePath(),StandardCopyOption.REPLACE_EXISTING);
        }finally{Files.deleteIfExists(temp);}
    }
    public static EditorDocument importZip(Path path) throws IOException {
        // Never extract supplied paths. Only the two known entries are read, with expansion limits.
        String json=null,manifest=null;int count=0;
        try(var zip=new ZipInputStream(Files.newInputStream(path))) {
            for(ZipEntry e;(e=zip.getNextEntry())!=null;) {
                if(++count>2||(!e.getName().equals(REPO_PATH)&&!e.getName().equals("kmenu-export-manifest.json")))throw new IOException("Unexpected export entry");
                byte[] bytes=zip.readNBytes(EditorDocument.MAX_BYTES+1);if(bytes.length>EditorDocument.MAX_BYTES)throw new IOException("Export exceeds size limit");
                String s=new String(bytes,StandardCharsets.UTF_8);
                if(e.getName().equals(REPO_PATH)){if(json!=null)throw new IOException("Duplicate document");json=s;}else {if(manifest!=null)throw new IOException("Duplicate manifest");manifest=s;}
            }
        }
        if(json==null||manifest==null)throw new IOException("Incomplete export");
        try{var m=EditorCodec.JSON.fromJson(manifest,com.google.gson.JsonObject.class);if(!hash(json.getBytes(StandardCharsets.UTF_8)).equals(m.get("sha256").getAsString())||!REPO_PATH.equals(m.get("path").getAsString()))throw new IOException("Export checksum mismatch");return EditorCodec.decode(json);}
        catch(RuntimeException e){throw new IOException("Invalid export",e);}
    }
    public static void exportRepository(EditorDocument d,Path repository) throws IOException {
        Path root=repository.toRealPath();if(!Files.isRegularFile(root.resolve("mod/build.gradle"))||!Files.exists(root.resolve(".git")))throw new IOException("Select the ForeverSkyblock Git repository root");
        Path parent=root.resolve("pack/config/forever_skyblock");Files.createDirectories(parent);
        if(!parent.toRealPath().startsWith(root))throw new IOException("Repository path escapes through a link");
        atomic(parent.resolve("kmenu.json"),EditorCodec.encode(d));
    }
    public static String hash(byte[] data){try{return java.util.HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(data));}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
}
