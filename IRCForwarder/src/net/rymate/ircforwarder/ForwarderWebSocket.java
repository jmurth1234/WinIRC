package net.rymate.ircforwarder;

import org.java_websocket.WebSocket;
import org.java_websocket.handshake.ClientHandshake;
import org.java_websocket.server.WebSocketServer;

import java.io.IOException;
import java.net.InetSocketAddress;
import java.util.HashMap;

/**
 * Created by Ryan on 07/12/2015.
 */
public class ForwarderWebSocket extends WebSocketServer {

    private final String ircServer;
    private final int ircPort;
    private final boolean ircSSL;
    private HashMap<WebSocket, ForwarderIrcSocket> sockets = new HashMap<>();


    public ForwarderWebSocket(int socketPort, String server, int port, boolean ssl) {
        super(new InetSocketAddress(socketPort));

        this.ircServer = server;
        this.ircPort = port;
        this.ircSSL = ssl;
    }

    @Override
    public void onOpen(WebSocket conn, ClientHandshake handshake) {
        ForwarderIrcSocket ircSocket = new ForwarderIrcSocket(ircServer, ircPort, ircSSL, conn);
        new Thread(() -> {
            try {
                ircSocket.connect();
            } catch (Exception e) {
                e.printStackTrace();
                try {
                    ircSocket.disconnect();
                } catch (Exception e1) {
                    e1.printStackTrace();
                }
            }
        }).start();


        sockets.put(conn, ircSocket);
    }

    @Override
    public void onClose(WebSocket conn, int code, String reason, boolean remote) {
        try {
            sockets.get(conn).disconnect();
            sockets.remove(conn);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    @Override
    public void onMessage(WebSocket conn, String message) {
        try {
            Thread.sleep(200);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
        sockets.get(conn).printLine(message);
    }

    @Override
    public void onError(WebSocket conn, Exception ex) {
        ex.printStackTrace();
    }
}
