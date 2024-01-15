package host.silas.anative.android.commonnativeevents;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileFilter;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStream;
import java.text.DecimalFormat;
import java.util.regex.Pattern;

public class AndroidDeviceInfo {
    private static String run(String[] cmd, String workdirectory)
            throws IOException {
        String result = "";
        try {
            ProcessBuilder builder = new ProcessBuilder(cmd);
            InputStream in = null;
            // 设置一个路径
            if (workdirectory != null) {
                builder.directory(new File(workdirectory));
                builder.redirectErrorStream(true);
                Process process = builder.start();
                in = process.getInputStream();
                if (in != null) {
                    byte[] re = new byte[1024];
                    while (in.read(re) != -1)
                        result = result + new String(re);
                }
            }
            if (in != null) {
                in.close();
            }
        } catch (Exception ex) {
            ex.printStackTrace();
        }
        return result;
    }

    // 获取CPU名字
    public static String GetCpuName() {
        try {
            FileReader fr = new FileReader("/proc/cpuinfo");
            BufferedReader br = new BufferedReader(fr);
            String text = br.readLine();
            br.close();
            String[] array = text.split(":\\s+", 2);
            return array[1];
        } catch (FileNotFoundException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
        return null;
    }

    // 获取CPU最大频率（单位KHZ）
    public static String GetMaxCpuFreq() {
        String result = "";
        int coreNum = GetCpuCoresNum();
        String onlineStr = "";
        try {
            StringBuffer cpuOnlineFilePath = new StringBuffer();
            StringBuffer cpufreqFilePath = new StringBuffer();
            for (int i = 0; i < coreNum; i++) {
                cpuOnlineFilePath.append("/sys/devices/system/cpu/cpu");
                cpuOnlineFilePath.append(i);
                cpuOnlineFilePath.append("/online");
                String[] args1 = { "/system/bin/cat",
                        cpuOnlineFilePath.toString() };
                cpuOnlineFilePath.setLength(0);
                onlineStr = run(args1, "/system/bin/");
                if (onlineStr.trim().equals("1")) {
                    cpufreqFilePath.append("/sys/devices/system/cpu/cpu");
                    cpufreqFilePath.append(i);
                    cpufreqFilePath.append("/cpufreq/cpuinfo_max_freq");
                    String[] args = { "/system/bin/cat",
                            cpufreqFilePath.toString() };
                    cpufreqFilePath.setLength(0);
                    String cmdResult = run(args, "/system/bin/");
                    result += (cmdResult.trim() + ":");
                }
            }
        } catch (IOException ex) {
            ex.printStackTrace();
        }
        return (result.length() > 0) ? result.substring(0, result.length() - 1) : result;
    }

    // 获取CPU最小频率（单位KHZ）
    public static String GetMinCpuFreq() {
        String result = "";
        int coreNum = GetCpuCoresNum();
        String onlineStr = "";
        try {
            StringBuffer cpuOnlineFilePath = new StringBuffer();
            StringBuffer cpufreqFilePath = new StringBuffer();
            for (int i = 0; i < coreNum; i++) {
                cpuOnlineFilePath.append("/sys/devices/system/cpu/cpu");
                cpuOnlineFilePath.append(i);
                cpuOnlineFilePath.append("/online");
                String[] args1 = { "/system/bin/cat",
                        cpuOnlineFilePath.toString() };
                cpuOnlineFilePath.setLength(0);
                onlineStr = run(args1, "/system/bin/");
                if (onlineStr.trim().equals("1")) {
                    cpufreqFilePath.append("/sys/devices/system/cpu/cpu");
                    cpufreqFilePath.append(i);
                    cpufreqFilePath.append("/cpufreq/cpuinfo_min_freq");
                    String[] args = { "/system/bin/cat",
                            cpufreqFilePath.toString() };
                    cpufreqFilePath.setLength(0);
                    String cmdResult = run(args, "/system/bin/");
                    result += (cmdResult.trim() + ":");
                }
            }
        } catch (IOException ex) {
            ex.printStackTrace();
        }
        return (result.length() > 0) ? result.substring(0, result.length() - 1) : result;
    }

    // 获取CPU当前频率（单位KHZ）
    public static String GetCurCpuFreq() {
        String result = "";
        int coreNum = GetCpuCoresNum();
        String onlineStr = "";
        try {
            StringBuffer cpuOnlineFilePath = new StringBuffer();
            StringBuffer cpufreqFilePath = new StringBuffer();
            for (int i = 0; i < coreNum; i++) {
                cpuOnlineFilePath.append("/sys/devices/system/cpu/cpu");
                cpuOnlineFilePath.append(i);
                cpuOnlineFilePath.append("/online");
                String[] args1 = { "/system/bin/cat",
                        cpuOnlineFilePath.toString() };
                cpuOnlineFilePath.setLength(0);
                onlineStr = run(args1, "/system/bin/");
                if (onlineStr.trim().equals("1")) {
                    cpufreqFilePath.append("/sys/devices/system/cpu/cpu");
                    cpufreqFilePath.append(i);
                    cpufreqFilePath.append("/cpufreq/scaling_cur_freq");
                    String[] args = { "/system/bin/cat",
                            cpufreqFilePath.toString() };
                    cpufreqFilePath.setLength(0);
                    String cmdResult = run(args, "/system/bin/");
                    result += (cmdResult.trim() + ":");
                }
            }
        } catch (IOException ex) {
            ex.printStackTrace();
        }
        return (result.length() > 0) ? result.substring(0, result.length() - 1) : result;
    }

    // 获取CPU核数（0表示单核，1表示双核...）
    public static int GetCpuKernel() {
        String result = null;
        try {
            String[] args = { "/system/bin/cat",
                    "/sys/devices/system/cpu/kernel_max" };
            result = run(args, "/system/bin/");
        } catch (IOException ex) {
            ex.printStackTrace();
        }
        return Integer.valueOf(result.trim()).intValue() + 1;
    }

    // 获取CPU核数
    public static int GetCpuCoresNum() {
        class CpuFilter implements FileFilter {
            @Override
            public boolean accept(File pathname) {
                if (Pattern.matches("cpu[0-9]", pathname.getName())) {
                    return true;
                }
                return false;
            }
        }
        try {
            File dir = new File("/sys/devices/system/cpu/");
            File[] files = dir.listFiles(new CpuFilter());
            return files.length;
        } catch (Exception e) {
            e.printStackTrace();
            // Default to return 1 core
            return 1;
        }
    }

    // 获取可用的RAM总大小,GB
    public static double GetMemTotal() {
        String result = null;
        String readLine = "";
        try {
            FileReader fr = new FileReader("/proc/meminfo");
            BufferedReader localBufferedReader = new BufferedReader(fr, 8192);
            while ((readLine = localBufferedReader.readLine()) != null) {
                String[] lineStr = readLine.split(":\\s+");
                if (lineStr[0].equals("MemTotal")) {
                    result = lineStr[1].split(" ")[0];
                    break;
                }
            }
            localBufferedReader.close();
        } catch (IOException e) {
            e.printStackTrace();
        }

        double totalMemG = Double.valueOf(result).doubleValue() / (1024 * 1024);
        return totalMemG;
    }
}