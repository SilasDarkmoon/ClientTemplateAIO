package host.silas.anative.android.commonnativeevents;

import android.app.ActivityManager;
import android.content.Context;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.net.wifi.WifiInfo;
import android.net.wifi.WifiManager;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings.Secure;
import android.telephony.TelephonyManager;
import android.util.DisplayMetrics;
import android.view.WindowManager;

import java.util.Locale;

import host.silas.anative.android.eventplugin.EventPlugin;

public class CommonNativeEvents {
    public static void Init() {
        EventPlugin.RegHandler("GetUDID", new Runnable() {
            @Override
            public void run() {
                String udid = GetUDID();

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(udid);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });
        EventPlugin.RegHandler("SDK_GetMetaData", new Runnable() {
            @Override
            public void run() {
                EventPlugin.GetParam(EventPlugin.TOKEN_ARGS, 0);
                final String key = EventPlugin.GetValStr();

                try {
                    ApplicationInfo info = EventPlugin.mainActivity.getPackageManager()
                            .getApplicationInfo(EventPlugin.mainActivity.getPackageName(),
                                    PackageManager.GET_META_DATA);
                    Bundle meta = info.metaData;
                    Object val = meta.get(key);

                    if (val != null) {
                        EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                        EventPlugin.SetValStr(val.toString());
                        EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                    }
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
        EventPlugin.RegHandler("SDK_GetChannel", new Runnable() {
            @Override
            public void run() {
                try {
                    ApplicationInfo info = EventPlugin.mainActivity.getPackageManager()
                            .getApplicationInfo(EventPlugin.mainActivity.getPackageName(),
                                    PackageManager.GET_META_DATA);
                    Bundle meta = info.metaData;
                    Object val = meta.get("DISTRIBUTE_CHANNEL");

                    if (val != null) {
                        EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                        EventPlugin.SetValStr(val.toString());
                        EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                    }
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
        EventPlugin.RegHandler("GetSysVersionNum", new Runnable() {
            @Override
            public void run() {
                String sysVersionNum = Build.VERSION.RELEASE + "";

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(sysVersionNum);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });
        EventPlugin.RegHandler("GetPhoneType", new Runnable() {
            @Override
            public void run() {
                String phoneType = Build.MODEL;

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(phoneType);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });
        EventPlugin.RegHandler("GetRatio", new Runnable() {
            @Override
            public void run() {
                DisplayMetrics dm = new DisplayMetrics();
                WindowManager windowMgr = (WindowManager) EventPlugin.mainActivity
                        .getSystemService(Context.WINDOW_SERVICE);
                windowMgr.getDefaultDisplay().getMetrics(dm);
                int width = dm.widthPixels;
                int height = dm.heightPixels;

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 2);
                EventPlugin.SetValNum(width);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                EventPlugin.SetValNum(height);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 1);
            }
        });
        EventPlugin.RegHandler("SDK_GetAppId", new Runnable() {
            @Override
            public void run() {
                try {
                    String name = EventPlugin.mainActivity.getPackageName();
                    EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                    EventPlugin.SetValStr(name);
                    EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });

        EventPlugin.RegHandler("SDK_GetAppName", new Runnable() {
            @Override
            public void run() {
                try {
                    String name = (String)EventPlugin.mainActivity.getPackageManager().getApplicationLabel(EventPlugin.mainActivity.getApplicationInfo());
                    EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                    EventPlugin.SetValStr(name);
                    EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });

        EventPlugin.RegHandler("SDK_GetAppVerName", new Runnable() {
            @Override
            public void run() {
                try {
                    PackageInfo info = EventPlugin.mainActivity.getPackageManager().getPackageInfo(EventPlugin.mainActivity.getPackageName(), 0);
                    String name = info.versionName;
                    EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                    EventPlugin.SetValStr(name);
                    EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });

        EventPlugin.RegHandler("SDK_GetAppVerCode", new Runnable() {
            @Override
            public void run() {
                try {
                    PackageInfo info = EventPlugin.mainActivity.getPackageManager().getPackageInfo(EventPlugin.mainActivity.getPackageName(), 0);
                    int code = info.versionCode;
                    EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                    EventPlugin.SetValNum(code);
                    EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
        EventPlugin.RegHandler("GetNetOperName", new Runnable() {
            @Override
            public void run() {
                TelephonyManager tm = (TelephonyManager) EventPlugin.mainActivity
                        .getSystemService(Context.TELEPHONY_SERVICE);
                String netOperName = tm.getNetworkOperatorName();

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(netOperName);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetNetType", new Runnable() {
            @Override
            public void run() {
                String netType = APNUtil.getNetworkType(EventPlugin.mainActivity);

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(netType);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });
        EventPlugin.RegHandler("GetMacAddr", new Runnable() {
            @Override
            public void run() {
                try {
                    WifiManager wifi = (WifiManager) EventPlugin.mainActivity
                            .getSystemService(Context.WIFI_SERVICE);
                    WifiInfo info = wifi.getConnectionInfo();
                    String macAddr = info.getMacAddress();

                    EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                    EventPlugin.SetValStr(macAddr);
                    EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                }
                catch (Exception e)
                {

                }
            }
        });

        EventPlugin.RegHandler("GetImei", new Runnable() {
            @Override
            public void run() {
                String imei = "";
                try
                {
                    imei = ((TelephonyManager) EventPlugin.mainActivity
                            .getSystemService(Context.TELEPHONY_SERVICE))
                            .getDeviceId();
                }
                catch (Exception e)
                {
                    //e.printStackTrace();
                    imei = "";
                }

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(imei);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetMemTotal", new Runnable() {
            @Override
            public void run() {
                double totalMem = AndroidDeviceInfo.GetMemTotal();
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValNum(totalMem);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetMemAvail", new Runnable() {
            @Override
            public void run() {
                final ActivityManager activityManager = ((ActivityManager) EventPlugin.mainActivity
                        .getSystemService(Context.ACTIVITY_SERVICE));
                ActivityManager.MemoryInfo meminfo = new ActivityManager.MemoryInfo();
                activityManager.getMemoryInfo(meminfo);
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValNum(meminfo.availMem >> 10);// KB
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetCpuName", new Runnable() {
            @Override
            public void run() {
                String name = AndroidDeviceInfo.GetCpuName();
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(name);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetMaxCpuFreq", new Runnable() {
            @Override
            public void run() {
                String maxCpuFreq = AndroidDeviceInfo.GetMaxCpuFreq();
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(maxCpuFreq);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetMinCpuFreq", new Runnable() {
            @Override
            public void run() {
                String minCpuFreq = AndroidDeviceInfo.GetMinCpuFreq();
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(minCpuFreq);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetCurCpuFreq", new Runnable() {
            @Override
            public void run() {
                String curCpuFreq = AndroidDeviceInfo.GetCurCpuFreq();
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValStr(curCpuFreq);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetCpuKernel", new Runnable() {
            @Override
            public void run() {
                int cpuKernal = AndroidDeviceInfo.GetCpuKernel();
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValNum(cpuKernal);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        EventPlugin.RegHandler("GetCpuCoresNum", new Runnable() {
            @Override
            public void run() {
                int cpuCores = AndroidDeviceInfo.GetCpuCoresNum();
                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 1);
                EventPlugin.SetValNum(cpuCores);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
            }
        });

        // 获得当前语言，国家码
        EventPlugin.RegHandler("GetLang", new Runnable() {

            @Override
            public void run() {
                Locale locale;
                Resources res = EventPlugin.mainActivity.getResources();
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                    locale = res.getConfiguration().getLocales().get(0);
                } else {
                    locale = res.getConfiguration().locale;
                }
                String language = locale.getLanguage();
                String countryCode = locale.getCountry();

                EventPlugin.SetParamCount(EventPlugin.TOKEN_RETS, 2);
                EventPlugin.SetValStr(language);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 0);
                EventPlugin.SetValStr(countryCode);
                EventPlugin.SetParam(EventPlugin.TOKEN_RETS, 1);
            }
        });
    }

    private static String SavedUDID;
    public static String GetUDID() {
        if (SavedUDID != null)
        {
            return SavedUDID;
        }

        String uuid = null;
        uuid = Secure.getString(EventPlugin.mainActivity.getContentResolver(),
                Secure.ANDROID_ID);
        if (uuid == null || uuid.equals("") || "9774d56d682e549c".equals(uuid)) {
            try
            {
                uuid = ((TelephonyManager) EventPlugin.mainActivity
                        .getSystemService(Context.TELEPHONY_SERVICE)).getDeviceId();
            }
            catch (Exception e)
            {
                //e.printStackTrace();
            }
        }

        SavedUDID = uuid == null ? "" : uuid;
        return SavedUDID;
    }
}
