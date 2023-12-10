using System;

public class InvalidChannelException : Exception
{
    public InvalidChannelException(ushort channelId)
    : base($"A channel with the id #{channelId} could not be found or is invalid!")
    { }

    public InvalidChannelException(string channelName)
    : base($"A channel with the name '{channelName}' could not be found or is invalid!")
    { }

    public InvalidChannelException(string channelName, ushort channelId)
    : base($"A channel with the id #{channelId} and name '{channelName}' could not be created!")
    { }
}