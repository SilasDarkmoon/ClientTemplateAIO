package host.silas.anative.android.sdkplugin;

import android.app.Activity;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.res.Configuration;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.MotionEvent;

import java.util.ArrayList;

public class SDKPlugin {
    public static Activity mainActivity;
    public static ArrayList<SDKPlugin> subPlugins = new ArrayList<SDKPlugin>();

    public static void LoadSubPlugins()
    {
        try
        {
            ApplicationInfo info = mainActivity.getPackageManager()
                    .getApplicationInfo(mainActivity.getPackageName(),
                            PackageManager.GET_META_DATA);
            Bundle meta = info.metaData;

            for (String key : meta.keySet())
            {
                if (key.startsWith("SDKPlugin_"))
                {
                    Object val = meta.get(key);
                    if (val instanceof String)
                    {
                        String clsname = (String)val;
                        try
                        {
                            Class cls = Class.forName(clsname);
                            if (cls != null)
                            {
                                Object instance = cls.newInstance();
                                if (instance instanceof SDKPlugin)
                                {
                                    SDKPlugin plugin = (SDKPlugin)instance;
                                    subPlugins.add(plugin);
                                }
                            }
                        }
                        catch (Exception e1)
                        {
                            e1.printStackTrace();
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            e.printStackTrace();
        }
    }
    public static void Init()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.InitEvents();
        }
    }
    public static void sPreOnCreate(Bundle args)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnCreate(args);
        }
    }
    public static void sPostOnCreate(Bundle args)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnCreate(args);
        }
    }
    public static void sPreOnNewIntent(Intent intent)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnNewIntent(intent);
        }
    }
    public static void sPostOnNewIntent(Intent intent)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnNewIntent(intent);
        }
    }
    public static void sPreOnDestroy()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnDestroy();
        }
    }
    public static void sPostOnDestroy()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnDestroy();
        }
    }
    public static void sPreOnPause()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnPause();
        }
    }
    public static void sPostOnPause()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnPause();
        }
    }
    public static void sPreOnResume()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnResume();
        }
    }
    public static void sPostOnResume()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnResume();
        }
    }
    public static void sPreOnLowMemory()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnLowMemory();
        }
    }
    public static void sPostOnLowMemory()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnLowMemory();
        }
    }
    public static void sPreOnTrimMemory(int condition)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnTrimMemory(condition);
        }
    }
    public static void sPostOnTrimMemory(int condition)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnTrimMemory(condition);
        }
    }
    public static void sPreOnConfigurationChanged(Configuration config)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnConfigurationChanged(config);
        }
    }
    public static void sPostOnConfigurationChanged(Configuration config)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnConfigurationChanged(config);
        }
    }
    public static void sPreOnWindowFocusChanged(boolean focus)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnWindowFocusChanged(focus);
        }
    }
    public static void sPostOnWindowFocusChanged(boolean focus)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnWindowFocusChanged(focus);
        }
    }
    public static boolean sPreDispatchKeyEvent(KeyEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PreDispatchKeyEvent(event)) return true;
        }
        return false;
    }
    public static boolean sPostDispatchKeyEvent(KeyEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PostDispatchKeyEvent(event)) return true;
        }
        return false;
    }
    public static boolean sPreOnKeyUp(int code, KeyEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PreOnKeyUp(code, event)) return true;
        }
        return false;
    }
    public static boolean sPostOnKeyUp(int code, KeyEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PostOnKeyUp(code, event)) return true;
        }
        return false;
    }
    public static boolean sPreOnKeyDown(int code, KeyEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PreOnKeyDown(code, event)) return true;
        }
        return false;
    }
    public static boolean sPostOnKeyDown(int code, KeyEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PostOnKeyDown(code, event)) return true;
        }
        return false;
    }
    public static boolean sPreOnTouchEvent(MotionEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PreOnTouchEvent(event)) return true;
        }
        return false;
    }
    public static boolean sPostOnTouchEvent(MotionEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PostOnTouchEvent(event)) return true;
        }
        return false;
    }
    public static boolean sPreOnGenericMotionEvent(MotionEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PreOnGenericMotionEvent(event)) return true;
        }
        return false;
    }
    public static boolean sPostOnGenericMotionEvent(MotionEvent event)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            if (plugin.PostOnGenericMotionEvent(event)) return true;
        }
        return false;
    }

    public static void sPreOnRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    public static void sPostOnRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    public static void sPreOnBackPressed()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnBackPressed();
        }
    }
    public static void sPostOnBackPressed()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnBackPressed();
        }
    }
    public static void sPreOnSaveInstanceState(Bundle outState)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnSaveInstanceState(outState);
        }
    }
    public static void sPostOnSaveInstanceState(Bundle outState)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnSaveInstanceState(outState);
        }
    }
    public static void sPreOnActivityResult(int requestCode, int resultCode, Intent data)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnActivityResult(requestCode, resultCode, data);
        }
    }
    public static void sPostOnActivityResult(int requestCode, int resultCode, Intent data)
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnActivityResult(requestCode, resultCode, data);
        }
    }
    public static void sPreOnStart()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnStart();
        }
    }
    public static void sPostOnStart()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnStart();
        }
    }
    public static void sPreOnRestart()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnRestart();
        }
    }
    public static void sPostOnRestart()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnRestart();
        }
    }
    public static void sPreOnStop()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PreOnStop();
        }
    }
    public static void sPostOnStop()
    {
        for (SDKPlugin plugin : subPlugins)
        {
            plugin.PostOnStop();
        }
    }

    // below are to be overrode by sub plugins
    public void InitEvents(){}
    public void PreOnCreate(Bundle args){}
    public void PostOnCreate(Bundle args){}
    public void PreOnNewIntent(Intent intent){}
    public void PostOnNewIntent(Intent intent){}
    public void PreOnDestroy(){}
    public void PostOnDestroy(){}
    public void PreOnPause(){}
    public void PostOnPause(){}
    public void PreOnResume(){}
    public void PostOnResume(){}
    public void PreOnLowMemory(){}
    public void PostOnLowMemory(){}
    public void PreOnTrimMemory(int condition){}
    public void PostOnTrimMemory(int condition){}
    public void PreOnConfigurationChanged(Configuration config){}
    public void PostOnConfigurationChanged(Configuration config){}
    public void PreOnWindowFocusChanged(boolean focus){}
    public void PostOnWindowFocusChanged(boolean focus){}
    public boolean PreDispatchKeyEvent(KeyEvent event){ return false; }
    public boolean PostDispatchKeyEvent(KeyEvent event){ return false; }
    public boolean PreOnKeyUp(int code, KeyEvent event){ return false; }
    public boolean PostOnKeyUp(int code, KeyEvent event){ return false; }
    public boolean PreOnKeyDown(int code, KeyEvent event){ return false; }
    public boolean PostOnKeyDown(int code, KeyEvent event){ return false; }
    public boolean PreOnTouchEvent(MotionEvent event){ return false; }
    public boolean PostOnTouchEvent(MotionEvent event){ return false; }
    public boolean PreOnGenericMotionEvent(MotionEvent event){ return false; }
    public boolean PostOnGenericMotionEvent(MotionEvent event){ return false; }

    public void PreOnRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults){}
    public void PostOnRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults){}
    public void PreOnBackPressed(){}
    public void PostOnBackPressed(){}
    public void PreOnSaveInstanceState(Bundle outState){}
    public void PostOnSaveInstanceState(Bundle outState){}
    public void PreOnActivityResult(int requestCode, int resultCode, Intent data){}
    public void PostOnActivityResult(int requestCode, int resultCode, Intent data){}
    public void PreOnStart(){}
    public void PostOnStart(){}
    public void PreOnRestart(){}
    public void PostOnRestart(){}
    public void PreOnStop(){}
    public void PostOnStop(){}
}
