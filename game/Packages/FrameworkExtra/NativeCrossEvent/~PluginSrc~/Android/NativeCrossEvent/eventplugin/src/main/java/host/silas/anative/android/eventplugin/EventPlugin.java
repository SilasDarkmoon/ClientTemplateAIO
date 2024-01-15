package host.silas.anative.android.eventplugin;

import android.app.Activity;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;

public class EventPlugin {
    public static void Init() {
        mainActivity = UnityPlayer.currentActivity;
    }

    public static Activity mainActivity;
    public static ThreadLocal<ArrayList<Object>> exJavaParams = new ThreadLocal<>();

    // Reg & Unreg
    public static native int RegHandler(String cate, Runnable runnable);

    public static native void UnregHandler(String cate, int refid);

    public static native void SetHandlerOrder(String cate, int refid, int order);

    // Call Other
    public static native void TrigEvent(String cate);

    // Get Value
    public static native int GetValType();

    public static native boolean GetValBool();

    public static native double GetValNum();

    public static native long GetValPtr();

    public static native String GetValStr();

    // Set Value
    public static native void SetValBool(boolean val);

    public static native void SetValNum(double num);

    public static native void SetValPtr(long ptr);

    public static native void SetValStr(String str);

    public static native void UnsetVal();

    // Get Params
    public static native int GetParamCount(int token);

    public static native void SetParamCount(int token, int cnt);

    public static native void GetParam(int token, int index);

    public static native void SetParam(int token, int index);

    public static native void GetParamName(int token, int index);

    public static native void SetParamName(int token, int index);

    // Global Val
    public static native void GetGlobal(String name);

    public static native void SetGlobal(String name);

    // Dict & List
    public static native int NewList();

    public static native void GetValListTo(int list);

    public static native void SetValList(int list);

    public static final int TOKEN_ARGS = 0;
    public static final int TOKEN_RETS = 1;
    public static final int TOKEN_CALL = 2;

    public static final int PARAM_TYPE_NULL = 0;
    public static final int PARAM_TYPE_BOOL = 1;
    public static final int PARAM_TYPE_NUMBER = 2;
    public static final int PARAM_TYPE_STRING = 3;
    public static final int PARAM_TYPE_LIST = 4;
    public static final int PARAM_TYPE_OBJECT = 5;

    private static void TrigJavaEvent(String cate, Object... args) {
        int token = TOKEN_CALL;
        int argcnt = args == null ? 0 : args.length;
        SetParamCount(token, argcnt);
        for (int i = 0; i < argcnt; ++i) {
            Object arg = args[i];
            if (arg instanceof Boolean) {
                SetValBool((Boolean) arg);
            } else if (arg instanceof Double) {
                SetValNum((Double) arg);
            } else if (arg instanceof Long) {
                SetValPtr((Long) arg);
            } else if (arg instanceof String) {
                SetValStr((String) arg);
            } else {
                UnsetVal();
            }
            SetParam(token, i);
        }
        TrigEvent(cate);
    }

    public static boolean TrigJavaEventBool(String cate, Object... args) {
        TrigJavaEvent(cate, args);
        GetParam(TOKEN_CALL, 0);
        return GetValBool();
    }

    public static double TrigJavaEventNum(String cate, Object... args) {
        TrigJavaEvent(cate, args);
        GetParam(TOKEN_CALL, 0);
        return GetValNum();
    }

    public static long TrigJavaEventPtr(String cate, Object... args) {
        TrigJavaEvent(cate, args);
        GetParam(TOKEN_CALL, 0);
        return GetValPtr();
    }

    public static String TrigJavaEventStr(String cate, Object... args) {
        TrigJavaEvent(cate, args);
        GetParam(TOKEN_CALL, 0);
        return GetValStr();
    }

    static {
        System.loadLibrary("EventPlugin");
    }
}
