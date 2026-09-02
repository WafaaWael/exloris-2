#import <UIKit/UIKit.h>
#include "UnityInterface.h"

static id ExlorisReduceMotionObserver = nil;

extern "C" bool ExlorisIsReduceMotionEnabled()
{
    return UIAccessibilityIsReduceMotionEnabled();
}

extern "C" void ExlorisStopReduceMotionObserver()
{
    if (ExlorisReduceMotionObserver != nil)
    {
        [[NSNotificationCenter defaultCenter] removeObserver:ExlorisReduceMotionObserver];
        ExlorisReduceMotionObserver = nil;
    }
}

extern "C" void ExlorisStartReduceMotionObserver()
{
    ExlorisStopReduceMotionObserver();
    ExlorisReduceMotionObserver = [[NSNotificationCenter defaultCenter]
        addObserverForName:UIAccessibilityReduceMotionStatusDidChangeNotification
        object:nil
        queue:[NSOperationQueue mainQueue]
        usingBlock:^(NSNotification* notification)
        {
            UnitySendMessage(
                "ExlorisAccessibilityBridge",
                "OnExlorisReduceMotionChanged",
                UIAccessibilityIsReduceMotionEnabled() ? "1" : "0");
        }];
}
