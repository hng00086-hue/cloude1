// Creado por mí — Equipo: Telpo TPS320
package com.equipo.telpobridge;

import android.content.Context;

import com.telpo.tps550.api.printer.UsbThermalPrinter;

import org.json.JSONArray;
import org.json.JSONObject;

/**
 * Imprime un ticket (el JSON de la API) en la impresora interna del Telpo TPS320.
 *
 * Usa la clase UsbThermalPrinter del SDK de Telpo. Si tu versión del SDK cambia algún
 * nombre de método, compáralo con la app demo que viene con el SDK.
 */
public class TelpoTicketPrinter {

    /** Caracteres por línea en papel de 58 mm con letra normal. */
    private static final int LINE_WIDTH = 32;
    private static final int SIZE_NORMAL = 24;
    private static final int SIZE_LARGE = 36;
    private static final int GRAY = 2;

    private final UsbThermalPrinter printer;

    public TelpoTicketPrinter(Context context) {
        this.printer = new UsbThermalPrinter(context.getApplicationContext());
    }

    /** Imprime el ticket; lanza una excepción con un mensaje legible si falla (sin papel, etc.). */
    public synchronized void print(JSONObject job) throws Exception {
        int copies = Math.max(1, job.optInt("copies", 1));
        JSONArray lines = job.getJSONArray("lines");

        printer.start(0);
        try {
            if (printer.checkStatus() == UsbThermalPrinter.STATUS_NO_PAPER) {
                throw new Exception("Sin papel");
            }
            for (int c = 0; c < copies; c++) {
                printer.reset();
                printer.setGray(GRAY);
                printer.setLeftIndent(0);
                printer.setLineSpace(0);
                for (int i = 0; i < lines.length(); i++) {
                    printLine(lines.getJSONObject(i));
                }
            }
        } finally {
            printer.stop();
        }
    }

    private void printLine(JSONObject line) throws Exception {
        String type = line.optString("type", "Text");

        if ("Feed".equals(type)) {
            printer.walkPaper(Math.max(1, line.optInt("lines", 1)) * 3);
            return;
        }

        String text = "Separator".equals(type)
                ? repeat('-', LINE_WIDTH)
                : line.optString("text", "");

        printer.setAlgin(toAlign(line.optString("align", "Left")));
        printer.setBold(line.optBoolean("bold", false));
        printer.setTextSize("Large".equals(line.optString("size")) ? SIZE_LARGE : SIZE_NORMAL);
        printer.addString(text);
        printer.printString();
    }

    private static int toAlign(String align) {
        if ("Center".equals(align)) return UsbThermalPrinter.ALGIN_MIDDLE;
        if ("Right".equals(align)) return UsbThermalPrinter.ALGIN_RIGHT;
        return UsbThermalPrinter.ALGIN_LEFT;
    }

    private static String repeat(char ch, int n) {
        StringBuilder sb = new StringBuilder(n);
        for (int i = 0; i < n; i++) sb.append(ch);
        return sb.toString();
    }
}
