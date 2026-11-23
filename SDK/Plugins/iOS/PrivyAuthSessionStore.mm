#import <Foundation/Foundation.h>
#import <Security/Security.h>
#include <stdlib.h>
#include <string.h>

static NSDictionary *PrivyAuthSessionQuery(const char *account) {
    if (account == NULL) return nil;
    NSString *name = [NSString stringWithUTF8String:account];
    if (name == nil) return nil;
    return @{
        (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
        (__bridge id)kSecAttrService: @"io.privy.unity.auth-session.v1",
        (__bridge id)kSecAttrAccount: name,
        (__bridge id)kSecAttrSynchronizable: @NO
    };
}

extern "C" {
    int PrivyAuthSessionRead(const char *account, char **value) {
        if (value == NULL) return errSecParam;
        *value = NULL;
        NSDictionary *query = PrivyAuthSessionQuery(account);
        if (query == nil) return errSecParam;
        NSMutableDictionary *readQuery = [query mutableCopy];
        readQuery[(__bridge id)kSecReturnData] = @YES;
        readQuery[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
        CFTypeRef result = NULL;
        OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)readQuery, &result);
        if (status != errSecSuccess) return status;
        NSData *data = CFBridgingRelease(result);
        NSString *text = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
        if (text == nil) return errSecDecode;
        *value = strdup([text UTF8String]);
        return *value == NULL ? errSecAllocate : errSecSuccess;
    }

    int PrivyAuthSessionWrite(const char *account, const char *value) {
        NSDictionary *query = PrivyAuthSessionQuery(account);
        if (query == nil || value == NULL) return errSecParam;
        NSString *text = [NSString stringWithUTF8String:value];
        if (text == nil) return errSecParam;
        NSDictionary *attributes = @{
            (__bridge id)kSecValueData: [text dataUsingEncoding:NSUTF8StringEncoding],
            (__bridge id)kSecAttrAccessible: (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        };
        OSStatus status = SecItemUpdate((__bridge CFDictionaryRef)query, (__bridge CFDictionaryRef)attributes);
        if (status != errSecItemNotFound) return status;
        NSMutableDictionary *newItem = [query mutableCopy];
        [newItem addEntriesFromDictionary:attributes];
        return SecItemAdd((__bridge CFDictionaryRef)newItem, NULL);
    }

    int PrivyAuthSessionDelete(const char *account) {
        NSDictionary *query = PrivyAuthSessionQuery(account);
        if (query == nil) return errSecParam;
        return SecItemDelete((__bridge CFDictionaryRef)query);
    }

    void PrivyAuthSessionFree(char *value) {
        if (value == NULL) return;
        memset_s(value, strlen(value), 0, strlen(value));
        free(value);
    }
}
