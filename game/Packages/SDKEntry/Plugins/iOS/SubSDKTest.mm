//
//  SubSDKTest.m
//  Unity-iPhone
//
//  Created by admin on 2020/6/18.
//

#import <Foundation/Foundation.h>
#import "SubSDK.h"

@interface SubSDKTest : SubSDK
@end

REG_SUBSDK(SubSDKTest)

@implementation SubSDKTest

- (void)initEvents
{
    ::printf("this is from SubSDKTest::initEvents\n");
}

- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
    ::printf("this is from SubSDKTest\n");
    ::printf("%p\n", EntryApplicationController);
    return YES;
}

- (BOOL)application:(UIApplication*)app openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options
{
    return YES;
}

- (void)applicationDidBecomeActive:(UIApplication*)application
{
}

@end
