package host.silas.anative.sdkpluginfdrecorder;

import android.system.Os;
import android.util.Log;

import java.io.BufferedReader;
import java.io.File;
import java.io.InputStreamReader;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.Map;
import java.util.Queue;

import host.silas.anative.android.sdkplugin.SDKPlugin;

public class SDKPluginFDRecorder extends SDKPlugin
{
    public SDKPluginFDRecorder()
    {
        StartRecorder();
    }

    private static ThreadLocal<File> fdPseudoFolder = new InheritableThreadLocal<>();
    private static Thread fdThread;
    private static Thread logThread;

    private static StringBuilder selectedLog = new StringBuilder();
    private static HashMap<String, String> curFDMap = new HashMap<>();

    public static class FDRecord
    {
        public Date Time;
        public String Target;
        public static SimpleDateFormat TimeFormat = new SimpleDateFormat("HH:mm:ss.SSSS");

        public FDRecord(Date time, String target)
        {
            Time = time;
            Target = target;
        }
        public FDRecord(String target)
        {
            Target = target;
            Time = new Date();
        }
    }
    private static HashMap<String, Queue<FDRecord>> fdRecord = new HashMap<>();
    private static HashMap<String, String> formattedFDRecord = null;

    private static boolean RecordFDList(HashMap<String, String> newmap)
    {
        synchronized (fdRecord) {
            HashMap<String, String> oldmap = curFDMap;
            curFDMap = newmap;
            boolean diff = false;
            for (Map.Entry<String, String> entry : newmap.entrySet()) {
                String k = entry.getKey();
                String v = entry.getValue();

                String oldv = oldmap.remove(k);
                if (v != null && !v.equals(oldv) || v == null && oldv != null)
                {
                    diff = true;
                    RecordFD(k, v);
                }
            }
            for (Map.Entry<String, String> entry : oldmap.entrySet()) {
                String k = entry.getKey();
                String v = entry.getValue();

                diff = true;
                RecordFD(k, null);
            }
            return diff;
        }
    }
    private static void RecordFD(String fd, String target)
    {
        Queue<FDRecord> queue = fdRecord.get(fd);
        if (queue == null)
        {
            queue = new LinkedList<>();
            fdRecord.put(fd, queue);
        }
        while (queue.size() >= 20)
        {
            queue.remove();
        }
        queue.add(new FDRecord(target));
    }
    private static boolean RecordFDList()
    {
        File fdFile = fdPseudoFolder.get();
        if (fdFile == null) {
            fdFile = new File("/proc/" + android.os.Process.myPid() + "/fd");
            fdPseudoFolder.set(fdFile);
        }
        File[] files = fdFile.listFiles();
        int length = files.length;
        HashMap<String, String> fdmap = new HashMap<>();
        for (int i = 0; i < length; ++i)
        {
            File file = files[i];
            String fd = file.getName();
            String realpath = null;
            try {
                realpath = Os.readlink(file.getAbsolutePath());
            } catch (Exception x) {
                realpath = x.getMessage();
            }
            fdmap.put(fd, realpath);
        }
        return RecordFDList(fdmap);
    }
    private static void FormatFDRecord()
    {
        HashMap<String, String> record = new HashMap<>();
        synchronized (fdRecord)
        {
            for (Map.Entry<String, Queue<FDRecord>> entry : fdRecord.entrySet())
            {
                String fd = entry.getKey();
                Queue<FDRecord> queue = entry.getValue();
                StringBuilder sb = new StringBuilder();
                for (FDRecord r : queue)
                {
                    sb.append("\t");
                    sb.append(FDRecord.TimeFormat.format(r.Time));
                    sb.append(" \t");
                    if (r.Target != null)
                    {
                        sb.append(r.Target);
                    }
                    else
                    {
                        sb.append("<null>");
                    }
                    sb.append("\n");
                }
                record.put(fd, sb.toString());
            }
            record.put("relative-log", selectedLog.toString());
            formattedFDRecord = record;
        }
    }
    private static boolean RecordAndFormatFDList()
    {
        if (RecordFDList()) {
            FormatFDRecord();
            return true;
        }
        return false;
    }

    public static void StartRecorder()
    {
        if (fdThread != null)
        {
            return;
        }
        fdThread = new Thread(new Runnable() {
            @Override
            public void run() {
                while (!Thread.interrupted())
                {
                    RecordAndFormatFDList();

                    try {
                        Thread.sleep(0);
                    } catch (InterruptedException e) {
                        return;
                    }
                }
            }
        });
        fdThread.start();

        logThread = new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    Process pro = null;
                    BufferedReader stream = null;
                    try {
                        pro = Runtime.getRuntime().exec("logcat");
                        stream = new BufferedReader(new InputStreamReader(pro.getInputStream()));
                        String line = null;
                        while ((line = stream.readLine()) != null)
                        {
                            line = line.toLowerCase();
                            if (!line.contains("nofdsan") && line.contains("fdsan"))
                            {
                                // found fdsan error, do some record.
                                synchronized (fdRecord)
                                {
                                    selectedLog.append(line);
                                    selectedLog.append("\n");

                                    RecordFDList();
                                    FormatFDRecord();
                                }
                                Log.e("SDKPluginFDRecorder", "Found and Recorded!");
                            }
                            if (Thread.interrupted())
                            {
                                return;
                            }
                        }
                    }
                    finally {
                        if (stream != null)
                        {
                            stream.close();
                        }
                        if (pro != null)
                        {
                            pro.destroy();
                        }
                    }
                }
                catch (Exception ex) {
                    ex.printStackTrace();
                }
            }
        });
        logThread.start();
    }

    public static boolean StopRecorder()
    {
        boolean aborted = false;
        if (fdThread != null)
        {
            fdThread.interrupt();
            fdThread = null;
            aborted = true;
        }
        if (logThread != null)
        {
            logThread.interrupt();
            logThread = null;
            aborted = true;
        }
        return aborted;
    }

    public static HashMap<String, String> GetFormattedRecord()
    {
        if (formattedFDRecord == null)
        {
            RecordFDList();
            FormatFDRecord();
        }
        else
        {
            //RecordAndFormatFDList();
        }
        return formattedFDRecord;
    }
}
