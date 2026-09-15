namespace LiarUtil.Core.Reanim.Flash;

internal static class DefaultBitmap
{
    private const string Base64 =
        "iVBORw0KGgoAAAANSUhEUgAAAGoAAAArCAIAAAAfR4oJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAEnQA" +
        "ABJ0Ad5mH3gAAAIASURBVGhD7ZbrrcIwDEaZqwMxD9OwDMNw2ziJ7eRLX1+vkCqff3EcPw4C8fgGBKGPIvRRhD6K0EcR+ihCH0Xo" +
        "owh9FKGPIvRRhD6K0EcR+ihW9b2fj5bp9cmXl/J5TbnBwvOdw9eS1qm1pSW5z7Y+20B8XqxQ9jDK5kDX9AqjP9cHdmVJBdeWuK7j" +
        "ZZ+DclTf9roHQT0c99LXx1Kk4ser2S7J5ogd7E/uLJIH5kKfqu1ZZ9DezTHhxtyh+tSXV0PSrrbpXNRx2id6Lo58H0Xu3Sbbc3WT" +
        "zE+myRdKVUxdGWy8C+KQPjBT18DHZKQmpRMigYUmc6HLhn1TVg2BN2UWDaZzPYKaMObZ1udwtXz/guuJB/C7ZrSZvzmhD9bv5nXH" +
        "5i4DGnkO//YpgyFdGE81CueLGVP2hL59Xe1xzy6In+hLKWC/hFzqLUgGc+3o2obt0T1XBmGF0DeY0kVxhUN1z+rr6reFXBvfs4Cj" +
        "BkYfvPcd0wmmjMs2awJ9fUgitSbqICnmFZh0dRcEpS9naIduL7m3sWbRlGEnHKmxkbaIHN2oYLBTf1xc2x5S30yZXWiySwUZLdOO" +
        "5CuAhpqgd/bRHE1H/7LrmQLavDkurO6CWNXHU/Tl4+0IfRShjyL0UfyzvrsT+ihCH0Xoowh9FKGPIvRRhD6K0EcR+ihCH8H3+weC" +
        "mUovKhX01QAAAABJRU5ErkJggg==";

    public static byte[] Bytes { get; } = Convert.FromBase64String(Base64);
}
