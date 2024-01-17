package host.silas.anative.android.sdkplugintest;

import android.util.Log;
import android.view.KeyEvent;

import host.silas.anative.android.sdkplugin.SDKPlugin;

public class SDKPluginTest extends SDKPlugin
{
    public SDKPluginTest()
    {
        Log.d("SDKPluginTest", ".ctor");
    }

    @Override
    public void InitEvents()
    {
        Log.d("SDKPluginTest", "InitEvents");
    }

    @Override
    public boolean PreDispatchKeyEvent(KeyEvent event)
    {
        Log.d("SDKPluginTest", "PreDispatchKeyEvent");
        Log.d("SDKPluginTest", ((Integer)event.getAction()).toString());
        return false;
    }
}
