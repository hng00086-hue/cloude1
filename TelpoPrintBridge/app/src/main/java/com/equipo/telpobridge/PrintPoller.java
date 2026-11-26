// Creado por mí — Equipo: Telpo TPS320
package com.equipo.telpobridge;

import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLEncoder;

/**
 * Pregunta a la API cada pocos segundos si hay tickets para este POS,
 * los imprime y confirma el resultado.
 */
public class PrintPoller implements Runnable {

    public interface Listener {
        void onLog(String message);
    }

    private static final int INTERVAL_MS = 3000;
    private static final int TIMEOUT_MS = 10000;

    private final String baseUrl;
    private final String deviceId;
    private final String deviceKey;
    private final TelpoTicketPrinter printer;
    private final Listener listener;
    private volatile boolean running = true;

    public PrintPoller(String baseUrl, String deviceId, String deviceKey,
                       TelpoTicketPrinter printer, Listener listener) {
        this.baseUrl = baseUrl.replaceAll("/+$", "");
        this.deviceId = deviceId;
        this.deviceKey = deviceKey;
        this.printer = printer;
        this.listener = listener;
    }

    public void stop() {
        running = false;
    }

    @Override
    public void run() {
        listener.onLog("Conectando a " + baseUrl + " como " + deviceId);
        while (running) {
            try {
                Response claim = post("/api/print-jobs/claim?deviceId=" + URLEncoder.encode(deviceId, "UTF-8"), null);
                if (claim.code == 200) {
                    handleJob(new JSONObject(claim.body));
                    continue; // puede haber más tickets en cola
                } else if (claim.code == 401) {
                    listener.onLog("La API rechazó la clave del POS (401)");
                } else if (claim.code != 204) {
                    listener.onLog("Respuesta inesperada de la API: " + claim.code);
                }
            } catch (Exception e) {
                listener.onLog("Sin conexión con la API: " + e.getMessage());
            }
            sleep(INTERVAL_MS);
        }
        listener.onLog("Detenido");
    }

    private void handleJob(JSONObject job) {
        String id = job.optString("id");
        try {
            printer.print(job);
            post("/api/print-jobs/" + id + "/done", null);
            listener.onLog("Impreso ticket " + id.substring(0, 8));
        } catch (Exception e) {
            String error = e.getMessage() != null ? e.getMessage() : e.getClass().getSimpleName();
            listener.onLog("Error imprimiendo " + id.substring(0, 8) + ": " + error);
            try {
                JSONObject body = new JSONObject().put("error", error);
                post("/api/print-jobs/" + id + "/failed", body.toString());
            } catch (Exception ignored) {
                // si no hay red, la API devolverá el ticket a la cola por tiempo vencido
            }
            sleep(INTERVAL_MS);
        }
    }

    private Response post(String path, String jsonBody) throws Exception {
        HttpURLConnection conn = (HttpURLConnection) new URL(baseUrl + path).openConnection();
        try {
            conn.setRequestMethod("POST");
            conn.setConnectTimeout(TIMEOUT_MS);
            conn.setReadTimeout(TIMEOUT_MS);
            if (deviceKey != null && !deviceKey.isEmpty()) {
                conn.setRequestProperty("X-Device-Key", deviceKey);
            }
            conn.setDoOutput(true);
            conn.setRequestProperty("Content-Type", "application/json");
            OutputStream out = conn.getOutputStream();
            out.write((jsonBody != null ? jsonBody : "").getBytes("UTF-8"));
            out.close();

            int code = conn.getResponseCode();
            InputStream in = code >= 400 ? conn.getErrorStream() : conn.getInputStream();
            return new Response(code, in != null ? readAll(in) : "");
        } finally {
            conn.disconnect();
        }
    }

    private static String readAll(InputStream in) throws Exception {
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        byte[] chunk = new byte[4096];
        int n;
        while ((n = in.read(chunk)) != -1) buf.write(chunk, 0, n);
        in.close();
        return buf.toString("UTF-8");
    }

    private void sleep(int ms) {
        try {
            Thread.sleep(ms);
        } catch (InterruptedException e) {
            running = false;
        }
    }

    private static class Response {
        final int code;
        final String body;

        Response(int code, String body) {
            this.code = code;
            this.body = body;
        }
    }
}
