# Demo: Chat

The chat demo implements a very simple chat client.

![Demo: Chat](../../.github/images/DemoChat.png)

## Details

Once a message is being send, it is send to, and only to, the host.  
The host then broadcasts the send message to every other client.

A message is not added to the chat-box, until the server informs us about the new message.  
Even if our local app send the message, it will only be added to the log once the server acknowledged it.
