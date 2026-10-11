package cn.local.starward;
import android.content.Context;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

public final class OfficialInbox {
    private static final int MAX=64*1024*1024;
    private static File dir(Context context){return new File(context.getFilesDir(),"official-inbox");}
    public static void put(Context context,String text)throws Exception{
        byte[] bytes=text.getBytes(StandardCharsets.UTF_8);if(bytes.length>MAX)throw new IOException("导出超过 64 MB");JSONObject raw=new JSONObject(text);if(!"XZY-RawBattleStats".equals(raw.getString("Format"))||raw.getInt("Version")!=1||raw.getJSONArray("Matches").length()>150000)throw new IOException("官网导出格式无效");
        File root=dir(context);if(!root.isDirectory()&&!root.mkdirs())throw new IOException("无法创建待导入文件夹");String id=UUID.randomUUID().toString();AtomicFile pending=new AtomicFile(new File(root,id+".pending"));FileOutputStream out=null;
        try{out=pending.startWrite();out.write(bytes);pending.finishWrite(out);out=null;if(!pending.getBaseFile().renameTo(new File(root,id+".json")))throw new IOException("无法提交待导入记录");}catch(Exception e){if(out!=null)pending.failWrite(out);throw e;}
    }
    public static String read(Context context)throws Exception{
        JSONArray result=new JSONArray();File[] files=dir(context).listFiles((directory,name)->name.matches("[0-9a-f-]{36}\\.json"));if(files==null)return result.toString();Arrays.sort(files,Comparator.comparingLong(File::lastModified));int total=0;
        for(File file:files){if(result.length()>=12)break;if(file.length()>MAX)throw new IOException("待导入文件超过 64 MB");if(total+file.length()>MAX)break;try(InputStream in=new FileInputStream(file);ByteArrayOutputStream out=new ByteArrayOutputStream()){byte[] b=new byte[8192];int n;while((n=in.read(b))!=-1){if(out.size()+n>MAX)throw new IOException("待导入文件超过 64 MB");out.write(b,0,n);}total+=out.size();JSONObject item=new JSONObject();item.put("Id",file.getName());item.put("Text",new String(out.toByteArray(),StandardCharsets.UTF_8));result.put(item);}}
        return result.toString();
    }
    public static void ack(Context context,String id)throws IOException{if(!id.matches("[0-9a-f-]{36}\\.json"))throw new IOException("待导入记录标识无效");File file=new File(dir(context),id);if(file.exists()&&!file.delete())throw new IOException("已保存，但待导入文件暂时无法清理");}
}
