namespace Produccion;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        txtQuery = new TextBox();
        btnEjecutar = new Button();
        dgvResultados = new DataGridView();
        lblEstado = new Label();
        ((System.ComponentModel.ISupportInitialize)dgvResultados).BeginInit();
        SuspendLayout();
        //
        // txtQuery
        //
        txtQuery.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtQuery.Location = new Point(12, 12);
        txtQuery.Multiline = true;
        txtQuery.Name = "txtQuery";
        txtQuery.Size = new Size(760, 60);
        txtQuery.TabIndex = 0;
        //
        // btnEjecutar
        //
        btnEjecutar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnEjecutar.Location = new Point(697, 78);
        btnEjecutar.Name = "btnEjecutar";
        btnEjecutar.Size = new Size(75, 23);
        btnEjecutar.TabIndex = 1;
        btnEjecutar.Text = "Ejecutar";
        btnEjecutar.UseVisualStyleBackColor = true;
        btnEjecutar.Click += btnEjecutar_Click;
        //
        // dgvResultados
        //
        dgvResultados.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvResultados.AllowUserToAddRows = false;
        dgvResultados.AllowUserToDeleteRows = false;
        dgvResultados.Location = new Point(12, 107);
        dgvResultados.Name = "dgvResultados";
        dgvResultados.ReadOnly = true;
        dgvResultados.Size = new Size(760, 320);
        dgvResultados.TabIndex = 2;
        //
        // lblEstado
        //
        lblEstado.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblEstado.AutoSize = true;
        lblEstado.Location = new Point(12, 440);
        lblEstado.Name = "lblEstado";
        lblEstado.Size = new Size(0, 15);
        lblEstado.TabIndex = 3;
        //
        // Form1
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(784, 461);
        Controls.Add(lblEstado);
        Controls.Add(dgvResultados);
        Controls.Add(btnEjecutar);
        Controls.Add(txtQuery);
        Name = "Form1";
        Text = "Producción — Conexión SAP";
        ((System.ComponentModel.ISupportInitialize)dgvResultados).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private TextBox txtQuery;
    private Button btnEjecutar;
    private DataGridView dgvResultados;
    private Label lblEstado;
}
