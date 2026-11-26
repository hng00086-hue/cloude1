// Creado por mí — Equipo: Telpo TPS320
package com.equipo.telpobridge;

import android.app.Activity;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.text.InputType;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

/** Pantalla de configuración: URL de la API, ID del POS y clave. */
public class MainActivity extends Activity {

    private EditText urlInput;
    private EditText deviceInput;
    private EditText keyInput;
    private TextView logView;
    private Button toggleButton;

    private TelpoTicketPrinter printer;
    private PrintPoller poller;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        printer = new TelpoTicketPrinter(this);

        SharedPreferences prefs = getSharedPreferences("config", MODE_PRIVATE);

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(24, 24, 24, 24);

        urlInput = field(root, "URL de la API (ej. http://192.168.10.20:5080)",
                prefs.getString("url", "http://192.168.10.20:5080"), InputType.TYPE_TEXT_VARIATION_URI);
        deviceInput = field(root, "ID del POS", prefs.getString("deviceId", "TPS320-01"), InputType.TYPE_CLASS_TEXT);
        keyInput = field(root, "Clave del POS (X-Device-Key)", prefs.getString("key", ""),
                InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);

        toggleButton = new Button(this);
        toggleButton.setText("Iniciar");
        toggleButton.setOnClickListener(v -> toggle());
        root.addView(toggleButton);

        Button testButton = new Button(this);
        testButton.setText("Imprimir prueba local");
        testButton.setOnClickListener(v -> printLocalTest());
        root.addView(testButton);

        logView = new TextView(this);
        ScrollView scroll = new ScrollView(this);
        scroll.addView(logView);
        root.addView(scroll, new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, 0, 1f));

        setContentView(root);

        // Arranca solo si ya estaba configurado, para que funcione al encender el POS
        if (prefs.getBoolean("running", false)) toggle();
    }

    private EditText field(LinearLayout root, String label, String value, int inputType) {
        TextView tv = new TextView(this);
        tv.setText(label);
        root.addView(tv);
        EditText et = new EditText(this);
        et.setSingleLine(true);
        et.setInputType(InputType.TYPE_CLASS_TEXT | inputType);
        et.setText(value);
        root.addView(et);
        return et;
    }

    private void toggle() {
        SharedPreferences.Editor prefs = getSharedPreferences("config", MODE_PRIVATE).edit();
        if (poller == null) {
            String url = urlInput.getText().toString().trim();
            String deviceId = deviceInput.getText().toString().trim();
            String key = keyInput.getText().toString().trim();
            prefs.putString("url", url).putString("deviceId", deviceId).putString("key", key)
                    .putBoolean("running", true).apply();

            poller = new PrintPoller(url, deviceId, key, printer, this::log);
            new Thread(poller, "print-poller").start();
            toggleButton.setText("Detener");
        } else {
            poller.stop();
            poller = null;
            prefs.putBoolean("running", false).apply();
            toggleButton.setText("Iniciar");
        }
    }

    /** Imprime sin la API, para comprobar que el SDK y la impresora funcionan. */
    private void printLocalTest() {
        new Thread(() -> {
            try {
                JSONArray lines = new JSONArray()
                        .put(new JSONObject().put("text", "PRUEBA LOCAL").put("align", "Center")
                                .put("bold", true).put("size", "Large"))
                        .put(new JSONObject().put("type", "Separator"))
                        .put(new JSONObject().put("text", "Impresora OK"))
                        .put(new JSONObject().put("type", "Feed").put("lines", 4));
                printer.print(new JSONObject().put("copies", 1).put("lines", lines));
                log("Prueba local impresa");
            } catch (Exception e) {
                log("Error en prueba local: " + e.getMessage());
            }
        }).start();
    }

    private void log(String message) {
        String time = new SimpleDateFormat("HH:mm:ss", Locale.getDefault()).format(new Date());
        runOnUiThread(() -> {
            logView.append(time + "  " + message + "\n");
            if (logView.getLineCount() > 200) logView.setText("");
        });
    }

    @Override
    protected void onDestroy() {
        if (poller != null) poller.stop();
        super.onDestroy();
    }
}
