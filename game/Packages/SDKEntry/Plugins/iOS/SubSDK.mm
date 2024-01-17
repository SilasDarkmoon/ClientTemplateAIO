//
//  SubSDK.m
//  Unity-iPhone
//
//  Created by admin on 2020/6/18.
//
#import "SubSDK.h"
#import "UnityAppController.h"

NSMutableArray* SubSDKs;
NSObject<UIApplicationDelegate>* EntryApplicationController;

@implementation SubSDK

+ (void)regSubSDK:(SubSDK*)sub
{
    if (SubSDKs == nil)
    {
        SubSDKs = [[NSMutableArray alloc] init];
    }
    [SubSDKs addObject:sub];
}

+ (void)initEventsStatic
{
    for (int i = 0; i < SubSDKs.count; ++i)
    {
        SubSDK* subsdk = [SubSDKs objectAtIndex:i];
        [subsdk initEvents];
    }
}
- (void)initEvents
{
}

+ (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptionsStatic:(NSDictionary*)launchOptions
{
    for (int i = 0; i < SubSDKs.count; ++i)
    {
        SubSDK* subsdk = [SubSDKs objectAtIndex:i];
        [subsdk application:application didFinishLaunchingWithOptions:launchOptions];
    }
    return YES;
}
- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
    return YES;
}

- (BOOL)application:(UIApplication*)app openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options
{
    return YES;
}

+ (BOOL)application:(UIApplication*)app openURLStatic:(NSURL*)url options:(NSDictionary<NSString*, id>*)options
{
    for (int i = 0; i < SubSDKs.count; ++i)
    {
        SubSDK* subsdk = [SubSDKs objectAtIndex:i];
        [subsdk application:app openURL:url options:options];
    }
    return YES;
}

- (void)applicationDidBecomeActive:(UIApplication*)application
{
}

+ (void)applicationDidBecomeActiveStatic:(UIApplication*)application
{
    for (int i = 0; i < SubSDKs.count; ++i)
    {
        SubSDK* subsdk = [SubSDKs objectAtIndex:i];
        [subsdk applicationDidBecomeActive:application];
    }
}

@end

extern "C"
{
void InitEventsForSubSDKs()
{
    [SubSDK initEventsStatic];
}
}

@interface SDKAppController : UnityAppController
@end
 
IMPL_APP_CONTROLLER_SUBCLASS (SDKAppController)

@implementation SDKAppController

- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
    ::printf("$this is from application didFinishLaunchingWithOptions!\n");
    EntryApplicationController = self;
    [SubSDK application:application didFinishLaunchingWithOptionsStatic:launchOptions];
    [super application:application didFinishLaunchingWithOptions:launchOptions];
    
    return YES;
}

- (BOOL)application:(UIApplication*)app openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options
{
    ::printf("$this is from application openURL!\n");
    [super application:app openURL:url options:options];
    [SubSDK application:app openURLStatic:url options:options];
    return YES;
}

- (void)applicationDidBecomeActive:(UIApplication*)application
{
    [super applicationDidBecomeActive:application];
    [SubSDK applicationDidBecomeActiveStatic:application];
}

@end
