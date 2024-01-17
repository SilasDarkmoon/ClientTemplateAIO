package host.silas.anative.android;

import android.content.Intent;
import android.content.res.Configuration;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.MotionEvent;

import com.unity3d.player.UnityPlayerActivity;

import host.silas.anative.android.sdkplugin.SDKPlugin;

public class EntryActivity extends UnityPlayerActivity
{
    protected void onCreate(Bundle args)
    {
        SDKPlugin.mainActivity = this;
        SDKPlugin.LoadSubPlugins();

        SDKPlugin.sPreOnCreate(args);
        super.onCreate(args);
        SDKPlugin.sPostOnCreate(args);
    }
    protected void onNewIntent(Intent intent)
    {
        SDKPlugin.sPreOnNewIntent(intent);
        super.onNewIntent(intent);
        SDKPlugin.sPostOnNewIntent(intent);
    }
    protected void onDestroy()
    {
        SDKPlugin.sPreOnDestroy();
        super.onDestroy();
        SDKPlugin.sPostOnDestroy();
    }
    protected void onPause()
    {
        SDKPlugin.sPreOnPause();
        super.onPause();
        SDKPlugin.sPostOnPause();
    }
    protected void onResume()
    {
        SDKPlugin.sPreOnResume();
        super.onResume();
        SDKPlugin.sPostOnResume();
    }
    public void onLowMemory()
    {
        SDKPlugin.sPreOnLowMemory();
        super.onLowMemory();
        SDKPlugin.sPostOnLowMemory();
    }
    public void onTrimMemory(int condition)
    {
        SDKPlugin.sPreOnTrimMemory(condition);
        super.onTrimMemory(condition);
        SDKPlugin.sPostOnTrimMemory(condition);
    }
    public void onConfigurationChanged(Configuration config)
    {
        SDKPlugin.sPreOnConfigurationChanged(config);
        super.onConfigurationChanged(config);
        SDKPlugin.sPostOnConfigurationChanged(config);
    }
    public void onWindowFocusChanged(boolean focus)
    {
        SDKPlugin.sPreOnWindowFocusChanged(focus);
        super.onWindowFocusChanged(focus);
        SDKPlugin.sPostOnWindowFocusChanged(focus);
    }
    public boolean dispatchKeyEvent(KeyEvent event)
    {
        if (SDKPlugin.sPreDispatchKeyEvent(event)) return true;
        if (super.dispatchKeyEvent(event)) return true;
        return SDKPlugin.sPostDispatchKeyEvent(event);
    }
    public boolean onKeyUp(int code, KeyEvent event)
    {
        if (SDKPlugin.sPreOnKeyUp(code, event)) return true;
        if (super.onKeyUp(code, event)) return true;
        return SDKPlugin.sPostOnKeyUp(code, event);
    }
    public boolean onKeyDown(int code, KeyEvent event)
    {
        if (SDKPlugin.sPreOnKeyDown(code, event)) return true;
        if (super.onKeyDown(code, event)) return true;
        return SDKPlugin.sPostOnKeyDown(code, event);
    }
    public boolean onTouchEvent(MotionEvent event)
    {
        if (SDKPlugin.sPreOnTouchEvent(event)) return true;
        if (super.onTouchEvent(event)) return true;
        return SDKPlugin.sPostOnTouchEvent(event);
    }
    public boolean onGenericMotionEvent(MotionEvent event)
    {
        if (SDKPlugin.sPreOnGenericMotionEvent(event)) return true;
        if (super.onGenericMotionEvent(event)) return true;
        return SDKPlugin.sPostOnGenericMotionEvent(event);
    }

    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults)
    {
        SDKPlugin.sPreOnRequestPermissionsResult(requestCode, permissions, grantResults);
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        SDKPlugin.sPostOnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
    public void onBackPressed()
    {
        SDKPlugin.sPreOnBackPressed();
        super.onBackPressed();
        SDKPlugin.sPostOnBackPressed();
    }
    protected void onSaveInstanceState(Bundle outState)
    {
        SDKPlugin.sPreOnSaveInstanceState(outState);
        super.onSaveInstanceState(outState);
        SDKPlugin.sPostOnSaveInstanceState(outState);
    }
    protected void onActivityResult(int requestCode, int resultCode, Intent data)
    {
        SDKPlugin.sPreOnActivityResult(requestCode, resultCode, data);
        super.onActivityResult(requestCode, resultCode, data);
        SDKPlugin.sPostOnActivityResult(requestCode, resultCode, data);
    }
    public void onStart()
    {
        SDKPlugin.sPreOnStart();
        super.onStart();
        SDKPlugin.sPostOnStart();
    }
    public void onRestart()
    {
        SDKPlugin.sPreOnRestart();
        super.onRestart();
        SDKPlugin.sPostOnRestart();
    }
    public void onStop()
    {
        SDKPlugin.sPreOnStop();
        super.onStop();
        SDKPlugin.sPostOnStop();
    }
}
