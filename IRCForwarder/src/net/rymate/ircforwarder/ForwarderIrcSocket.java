package net.rymate.ircforwarder;

import org.java_websocket.WebSocket;
import sun.security.ssl.SSLSocketImpl;

import javax.net.ssl.SSLSocket;
import javax.net.ssl.SSLSocketFactory;
import java.io.*;
import java.net.Socket;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

/**
 * Created by Ryan on 07/12/2015.
 */
public class ForwarderIrcSocket {
    private final String ircServer;
    private final int ircPort;
    private final boolean ircSSL;
    private final WebSocket webSocket;
    private BufferedReader reader;
    private BufferedWriter writer;
    private Socket socket;
    private boolean working = true;

    public ForwarderIrcSocket(String ircServer, int ircPort, boolean ircSSL, WebSocket conn) {
        this.ircServer = ircServer;
        this.ircPort = ircPort;
        this.ircSSL = ircSSL;
        this.webSocket = conn;
    }

    public void connect() throws Exception {
        // Connect directly to the IRC server.
        socket = new Socket(ircServer, ircPort);

        reader = new BufferedReader(
                new InputStreamReader(socket.getInputStream()));

        writer = new BufferedWriter(
                new OutputStreamWriter(socket.getOutputStream()));

        webSocket.send("SOCKET_CONNECTED_IRC");

        Runnable helloRunnable = new Runnable() {
            public void run() {
                webSocket.send("PING ");
            }
        };

        ScheduledExecutorService executor = Executors.newScheduledThreadPool(1);
        executor.scheduleAtFixedRate(helloRunnable, 0, 30, TimeUnit.SECONDS);

        String line = null;
        while (working) {
            line = reader.readLine( );
            if (line != null) {
                webSocket.send(line);
            }
        }
    }

    public void printLine(String s) {
        try {
            writer.write(s);
            writer.newLine();
            writer.flush();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public void disconnect() throws Exception {
        working = false;
        socket.close();
    }
}
