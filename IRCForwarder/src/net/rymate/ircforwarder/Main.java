package net.rymate.ircforwarder;

import org.java_websocket.drafts.Draft_17;

import java.net.UnknownHostException;

public class Main {

    public static void main(String[] args) {
        if (args.length != 3) {
            System.out.println("Arguments: <irc hostname> <irc port> <websocket port>");
            return;
        }

        String server = args[0];
        int port = Integer.parseInt(args[1]);
        //boolean ssl = Boolean.parseBoolean(args[2]);
        boolean ssl = false;
        int socketPort = Integer.parseInt(args[2]);
        System.out.println("Starting WebSocket server...");
        ForwarderWebSocket socket = new ForwarderWebSocket(socketPort, server, port, ssl);
        socket.start();
        System.out.println("Started WebSocket server!");
    }
}
