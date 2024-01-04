#!/bin/bash

# LuaJIT 的源码路径
LUAJIT=.
BUILD_DIR=$LUAJIT/build

# #编译 android-x86
# make clean
# NDK=`pwd`/ndk
# NDKAPI=14
# NDKTRIPLE=x86
# NDKVER=$NDK/toolchains/$NDKTRIPLE-4.9
# NDKP=$NDKVER/prebuilt/linux-x86_64/bin/i686-linux-android-
# NDKF="-isystem $NDK/sysroot/usr/include/i686-linux-android -D__ANDROID_API__=$NDKAPI"
# NDK_SYSROOT_BUILD=$NDK/sysroot
# NDK_SYSROOT_LINK=$NDK/platforms/android-$NDKAPI/arch-x86

# make HOST_CC="gcc -m32 " CROSS=$NDKP TARGET_FLAGS="$NDKF" TARGET_SYS=Linux TARGET_SHLDFLAGS="--sysroot $NDK_SYSROOT_LINK"  TARGET_LDFLAGS="--sysroot $NDK_SYSROOT_LINK" TARGET_CFLAGS="--sysroot $NDK_SYSROOT_BUILD" TARGET_LFSFLAGS=""

# OUTPUT=$BUILD_DIR/android/x86
# rm -rf $OUTPUT
# mkdir -p $OUTPUT
# mv ./src/libluajit.so $OUTPUT/libluajit.so

# #编译 android-armeabi
# make clean

# NDK=`pwd`/ndk
# NDKAPI=14
# NDKTRIPLE=arm-linux-androideabi
# NDKVER=$NDK/toolchains/$NDKTRIPLE-4.9
# NDKP=$NDKVER/prebuilt/linux-x86_64/bin/$NDKTRIPLE-
# NDKF="-isystem $NDK/sysroot/usr/include/$NDKTRIPLE -D__ANDROID_API__=$NDKAPI"
# NDK_SYSROOT_BUILD=$NDK/sysroot
# NDK_SYSROOT_LINK=$NDK/platforms/android-$NDKAPI/arch-arm

# make HOST_CC="gcc -m32 " CROSS=$NDKP TARGET_FLAGS="$NDKF" TARGET_SYS=Linux TARGET_SHLDFLAGS="--sysroot $NDK_SYSROOT_LINK"  TARGET_LDFLAGS="--sysroot $NDK_SYSROOT_LINK" TARGET_CFLAGS="--sysroot $NDK_SYSROOT_BUILD" TARGET_LFSFLAGS=""

# OUTPUT=$BUILD_DIR/android/armeabi
# rm -rf $OUTPUT
# mkdir -p $OUTPUT
# mv ./src/libluajit.so $OUTPUT/libluajit.so

# #编译 android-armeabi-v7a
# make clean
# NDK=`pwd`/ndk
# NDKAPI=14
# NDKTRIPLE=arm-linux-androideabi
# NDKVER=$NDK/toolchains/$NDKTRIPLE-4.9
# NDKP=$NDKVER/prebuilt/linux-x86_64/bin/$NDKTRIPLE-
# NDKF="-isystem $NDK/sysroot/usr/include/$NDKTRIPLE -D__ANDROID_API__=$NDKAPI"
# NDK_SYSROOT_BUILD=$NDK/sysroot
# NDK_SYSROOT_LINK=$NDK/platforms/android-$NDKAPI/arch-arm
# NDKARCH="-march=armv7-a -mfloat-abi=softfp -Wl,--fix-cortex-a8"

# make HOST_CC="gcc -m32 " CROSS=$NDKP TARGET_FLAGS="$NDKF $NDKARCH" TARGET_SYS=Linux TARGET_SHLDFLAGS="--sysroot $NDK_SYSROOT_LINK"  TARGET_LDFLAGS="--sysroot $NDK_SYSROOT_LINK" TARGET_CFLAGS="--sysroot $NDK_SYSROOT_BUILD" TARGET_LFSFLAGS=""

# OUTPUT=$BUILD_DIR/android/armeabi-v7a
# rm -rf $OUTPUT
# mkdir -p $OUTPUT
# mv ./src/libluajit.so $OUTPUT/libluajit.so

#编译 android-arm64-v8a
make clean

NDK=`pwd`/ndk
NDKAPI=21
NDKTRIPLE=aarch64-linux-android
NDKVER=$NDK/toolchains/$NDKTRIPLE-4.9
NDKP=$NDKVER/prebuilt/linux-x86_64/bin/$NDKTRIPLE-
NDKF="-isystem $NDK/sysroot/usr/include/$NDKTRIPLE -D__ANDROID_API__=$NDKAPI"
NDK_SYSROOT_BUILD=$NDK/sysroot
NDK_SYSROOT_LINK=$NDK/platforms/android-$NDKAPI/arch-arm64

make HOST_CC="gcc " CROSS=$NDKP TARGET_FLAGS="$NDKF" TARGET_SYS=Linux TARGET_SHLDFLAGS="--sysroot $NDK_SYSROOT_LINK"  TARGET_LDFLAGS="--sysroot $NDK_SYSROOT_LINK" TARGET_CFLAGS="--sysroot $NDK_SYSROOT_BUILD" TARGET_LFSFLAGS=""

OUTPUT=$BUILD_DIR/android/arm64-v8a
rm -rf $OUTPUT
mkdir -p $OUTPUT
mv ./src/libluajit.so $OUTPUT/libluajit.so

make clean
