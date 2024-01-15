//
//  CommonNativeEvents.m
//  CommonNativeEvents
//
//  Created by Silas on 2018/7/6.
//  Copyright © 2018年 Silas. All rights reserved.
//

#include "CommonNativeEvents.h"
#include "EventPlugin.h"

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AdSupport/ASIdentifierManager.h>

#include <sys/types.h>
#include <sys/param.h>
#include <sys/ioctl.h>
#include <sys/socket.h>
#include <net/if.h>
#include <netinet/in.h>
#include <net/if_dl.h>
#include <sys/sysctl.h>

static void GetUDID(const char* cate);
static void GetIDFV(const char* cate);
static void GetEncryptKey(const char* cate);
static void GetMetaData(const char* cate);
static void GetChannel(const char* cate);
static void GetSysVersionNum(const char* cate);
static void GetPhoneType(const char* cate);
static void GetRatio(const char* cate);
static void GetAppId(const char* cate);
static void GetAppName(const char* cate);
static void GetAppVerName(const char* cate);
static void GetAppVerCode(const char* cate);
static void GetLang(const char* cate);
NSString* Global_GetIDFV(void);

void Init_CommonNativeEvents()
{
    RegHandler("GetUDID", GetUDID);
    RegHandler("GetIDFV", GetIDFV);
    RegHandler("Get_EncryptKey", GetEncryptKey);
    RegHandler("SDK_GetMetaData",GetMetaData);
    RegHandler("SDK_GetChannel",GetChannel);
    RegHandler("GetSysVersionNum", GetSysVersionNum);
    RegHandler("GetPhoneType", GetPhoneType);
    RegHandler("GetRatio", GetRatio);
    RegHandler("SDK_GetAppId", GetAppId);
    RegHandler("SDK_GetAppName", GetAppName);
    RegHandler("SDK_GetAppVerName", GetAppVerName);
    RegHandler("SDK_GetAppVerCode", GetAppVerCode);
    RegHandler("GetLang", GetLang);
}

static NSString* idfa();
static NSString* mac();

static void GetUDID(const char* cate)
{
    NSString* udid = nil;
    if ([[[UIDevice currentDevice] systemVersion] intValue] >= 6) {
        udid = idfa();
    }
    else{
        udid = mac();
    }
    
    NSLog(@"idfa:%@",udid);
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(udid);
    SetParam(TOKEN_RETS, 0);
}
NSString* Global_GetIDFV(void)
{
    NSString *idfv = [[[UIDevice currentDevice] identifierForVendor] UUIDString];
    return idfv;
}
static void GetIDFV(const char* cate)
{
    NSString* idfv = Global_GetIDFV();
    
    NSLog(@"idfv:%@", idfv);
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(idfv);
    SetParam(TOKEN_RETS, 0);
}
static void GetAppId(const char* cate)
{
    NSString* appid = [NSBundle mainBundle].bundleIdentifier;
    
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(appid);
    SetParam(TOKEN_RETS, 0);
}
static void GetAppName(const char* cate)
{
    NSString* name = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleDisplayName"];
    
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(name);
    SetParam(TOKEN_RETS, 0);
}
static void GetAppVerName(const char* cate)
{
    NSString* name = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleShortVersionString"];
    
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(name);
    SetParam(TOKEN_RETS, 0);
}
static void GetAppVerCode(const char* cate)
{
    NSString* name = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleVersion"];
    
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(name);
    SetParam(TOKEN_RETS, 0);
}
static void GetEncryptKey(const char* cate)
{
    const char* key = "6uQ0y4MOtWovdWqkI8ZaiyT4dPVnGU0y";
    
    SetParamCount(TOKEN_RETS, 1);
    SetValStr(key);
    SetParam(TOKEN_RETS, 0);
}
static void GetMetaData(const char* cate)
{
    GetParam(TOKEN_ARGS, 0);
    NSString* key = [NSString stringWithCString:GetValStr() encoding: NSUTF8StringEncoding];
    id value = [[NSBundle mainBundle]objectForInfoDictionaryKey:key];
    
    
    if (value && [value isKindOfClass:[NSString class]]) {
        NSLog(@"Info.plist,read:%@,return:%@",key,value);
        
        SetParamCount(TOKEN_RETS, 1);
        SetValStr([value UTF8String]);
        SetParam(TOKEN_RETS, 0);
    }
}
static void GetChannel(const char* cate)
{
    NSString* value =[[NSBundle mainBundle]objectForInfoDictionaryKey:@"CHANNEL"];
    
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(value);
    SetParam(TOKEN_RETS, 0);
}
static void GetSysVersionNum(const char* cate)
{
    NSString* ver = [[UIDevice currentDevice] systemVersion];
    
    SetParamCount(TOKEN_RETS, 1);
    SetValNSString(ver);
    SetParam(TOKEN_RETS, 0);
}
static void GetPhoneType(const char* cate)
{
    size_t size;
    sysctlbyname("hw.machine", NULL, &size, NULL, 0);
    char* machine = (char*)alloca(size + 1);
    memset(machine, 0, size + 1);
    sysctlbyname("hw.machine", machine, &size, NULL, 0);
    
    SetParamCount(TOKEN_RETS, 1);
    SetValStr(machine);
    SetParam(TOKEN_RETS, 0);
}
static void GetRatio(const char* cate)
{
    CGSize size = [[UIScreen mainScreen] bounds].size;
    float scale = [UIScreen mainScreen].scale;
    
    SetParamCount(TOKEN_RETS, 3);
    SetValNum(size.width * scale);
    SetParam(TOKEN_RETS, 0);
    SetValNum(size.height * scale);
    SetParam(TOKEN_RETS, 1);
    SetValNum(scale);
    SetParam(TOKEN_RETS, 2);
}

// 地方型号  （国际化区域名称）
static void GetLang(const char* cate)
{
    //Getting the User’s Language
    //    NSArray *languageArray = [NSLocale preferredLanguages];
    //    NSString *language2 = [languageArray objectAtIndex:0];
    //    NSLog(@"language2 = %@", language2);
    
    // 获取语言
    NSString* language = [[NSLocale currentLocale] objectForKey:NSLocaleLanguageCode];
    
    // 国家code 英国:GB 美国:US
    NSString* countryCode = [[NSLocale currentLocale] objectForKey:NSLocaleCountryCode];
    
    //    NSString *country = [[NSLocale currentLocale] localeIdentifier];
    
    SetParamCount(TOKEN_RETS, 2);
    SetValNSString(language);
    SetParam(TOKEN_RETS, 0);
    SetValNSString(countryCode);
    SetParam(TOKEN_RETS, 1);
    //    SetValStr([country UTF8String]);
    //    SetParam(LETRETS, 2);
}


static NSString* idfa()
{
    double version = [[UIDevice currentDevice].systemVersion doubleValue];
    if (version<6.0f) {
        return @"5.0-noidfa";
    }
    NSString *aduid = [[[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString] stringByAddingPercentEscapesUsingEncoding:NSUTF8StringEncoding];
    BOOL result = [@"00000000-0000-0000-0000-000000000000" isEqualToString:aduid];
    if (result)
    {
        return NULL;
    }
    return aduid;
}
static NSString* mac(void)
{
    int                 mib[6];
    size_t              len;
    void                *buf;
    unsigned char       *ptr;
    struct if_msghdr    *ifm;
    struct sockaddr_dl  *sdl;
    
    mib[0] = CTL_NET;
    mib[1] = AF_ROUTE;
    mib[2] = 0;
    mib[3] = AF_LINK;
    mib[4] = NET_RT_IFLIST;
    
    if ((mib[5] = if_nametoindex("en0")) == 0) {
        printf("Error: if_nametoindex error/n");
        return NULL;
    }
    if (sysctl(mib, 6, NULL, &len, NULL, 0) < 0) {
        printf("Error: sysctl, take 1/n");
        return NULL;
    }
    if ((buf = malloc(len)) == NULL) {
        printf("Could not allocate memory. error!/n");
        return NULL;
    }
    if (sysctl(mib, 6, buf, &len, NULL, 0) < 0) {
        printf("Error: sysctl, take 2");
        return NULL;
    }
    ifm = (struct if_msghdr *)buf;
    sdl = (struct sockaddr_dl *)(ifm + 1);
    ptr = (unsigned char *)LLADDR(sdl);
    NSString *outstring = [NSString stringWithFormat:@"%02x:%02x:%02x:%02x:%02x:%02x", *ptr, *(ptr+1), *(ptr+2), *(ptr+3), *(ptr+4), *(ptr+5)];
    free(buf);
    return outstring;
}
