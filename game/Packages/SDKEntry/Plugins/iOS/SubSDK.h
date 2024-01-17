#pragma once

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

extern NSMutableArray* SubSDKs;
extern NSObject<UIApplicationDelegate>* EntryApplicationController;

#define REG_SUBSDK(ClassName)                       \
@interface ClassName(RegSubSDKDelegate)             \
+(void)load;                                        \
@end                                                \
@implementation ClassName(RegSubSDKDelegate)        \
+(void)load                                         \
{                                                   \
    [SubSDK regSubSDK:[[ClassName alloc] init]];    \
}                                                   \
@end                                                \

@interface SubSDK : NSObject
+ (void)regSubSDK:(SubSDK*)sub;

+ (void)initEventsStatic;
- (void)initEvents;

+ (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptionsStatic:(NSDictionary*)launchOptions;
- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions;
- (BOOL)application:(UIApplication*)app openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options;
+ (BOOL)application:(UIApplication*)app openURLStatic:(NSURL*)url options:(NSDictionary<NSString*, id>*)options;
- (void)applicationDidBecomeActive:(UIApplication*)application;
+ (void)applicationDidBecomeActiveStatic:(UIApplication*)application;
@end

