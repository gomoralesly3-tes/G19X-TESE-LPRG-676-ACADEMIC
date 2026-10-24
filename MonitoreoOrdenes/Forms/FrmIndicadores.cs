using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;

namespace MonitoreoOrdenes.Forms
{
    /// <summary>
    /// Indicadores de cumplimiento y tiempos de entrega por proveedor, riesgo de las órdenes
    /// abiertas y tiempo en cada estatus. El riesgo se calcula con reglas explicables
    /// (ver sp_RiesgoOrdenesAbiertas): cada punto sumado trae su motivo.
    /// </summary>
    public class FrmIndicadores : Form
    {
        private readonly IndicadoresRepository _repo = new IndicadoresRepository();

        private ComboBox     cboPeriodo;
        private Label        lblHallazgos;
        private DataGridView dgvProv, dgvRiesgo, dgvTiempos;

        private class Periodo
        {
            public string Texto;
            public int?   Dias;
            public override string ToString() { return Texto; }
        }

        public FrmIndicadores()
        {
            this.Text          = "Indicadores y riesgo";
            this.Size          = new Size(1180, 740);
            this.MinimumSize   = new Size(980, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor     = EstiloAdmin.Fondo;

            // ── Barra: periodo y actualizar ──
            var barra = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White };
            barra.Controls.Add(EstiloAdmin.Etiqueta("Periodo de los indicadores de proveedores:", 20, 19));
            cboPeriodo = new ComboBox
            {
                Location      = new Point(310, 14),
                Size          = new Size(200, 28),
                Font          = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboPeriodo.Items.Add(new Periodo { Texto = "Todo el historial" });
            cboPeriodo.Items.Add(new Periodo { Texto = "Últimos 30 días",  Dias = 30 });
            cboPeriodo.Items.Add(new Periodo { Texto = "Últimos 90 días",  Dias = 90 });
            cboPeriodo.Items.Add(new Periodo { Texto = "Últimos 180 días", Dias = 180 });
            cboPeriodo.Items.Add(new Periodo { Texto = "Últimos 365 días", Dias = 365 });
            cboPeriodo.SelectedIndex = 0;
            cboPeriodo.SelectionChangeCommitted += (s, e) => Cargar();
            var btnActualizar = EstiloAdmin.Boton("Actualizar", 530, 11, 110, 34, EstiloAdmin.Azul);
            btnActualizar.Click += (s, e) => Cargar();
            barra.Controls.AddRange(new Control[] { cboPeriodo, btnActualizar });

            // ── Hallazgos automáticos ──
            var panelHallazgos = new Panel { Dock = DockStyle.Top, Height = 170, BackColor = Color.White, Padding = new Padding(20, 10, 20, 10) };
            var tituloHallazgos = new Label
            {
                Text      = "Hallazgos",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = EstiloAdmin.Azul,
                Location  = new Point(20, 8),
                AutoSize  = true
            };
            lblHallazgos = new Label
            {
                Location  = new Point(20, 36),
                Size      = new Size(1100, 128),
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelHallazgos.Controls.AddRange(new Control[] { tituloHallazgos, lblHallazgos });

            // ── Pestañas ──
            dgvProv    = NuevaGrid();
            dgvRiesgo  = NuevaGrid();
            dgvTiempos = NuevaGrid();

            AgregarColumnas(dgvProv,
                "Proveedor", "Órdenes", "Entregadas", "A tiempo", "% Cumplimiento",
                "Entrega prom. (días)", "Retraso prom. (días)", "Vencidas abiertas",
                "Incidencias", "Incid. abiertas");
            dgvProv.Columns["Proveedor"].FillWeight = 220;

            AgregarColumnas(dgvRiesgo,
                "Orden", "Proveedor", "Estatus", "Vence (días)", "Sin cambios (días)",
                "Incid. abiertas", "Cumpl. proveedor", "Riesgo", "Motivos");
            dgvRiesgo.Columns["Motivos"].FillWeight = 420;
            dgvRiesgo.Columns["Proveedor"].FillWeight = 150;

            AgregarColumnas(dgvTiempos, "Estatus", "Estancias", "Días promedio", "Días máximo");
            dgvTiempos.Columns["Estatus"].FillWeight = 200;

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };
            tabs.TabPages.Add(CrearPestana("Proveedores", dgvProv,
                "Solo órdenes enviadas (sin borradores, pendientes de aprobación ni canceladas). " +
                "Cumplimiento = entregas a tiempo ÷ entregas realizadas."));
            tabs.TabPages.Add(CrearPestana("Riesgo de órdenes abiertas", dgvRiesgo,
                "Puntaje de 0 a 100 con reglas: vencimiento (hasta 50), días sin cambio de estatus (15), " +
                "incidencias abiertas (20), cumplimiento histórico del proveedor (15) y prioridad alta (5). " +
                "Alto: 60 o más · Medio: 30 a 59."));
            tabs.TabPages.Add(CrearPestana("Tiempo por estatus", dgvTiempos,
                "Días promedio que las órdenes permanecen en cada estatus, según el historial de cambios. " +
                "Los estatus con promedios altos son los cuellos de botella del proceso."));

            // El control agregado al final queda arriba del todo
            this.Controls.Add(tabs);
            this.Controls.Add(panelHallazgos);
            this.Controls.Add(barra);
            this.Controls.Add(EstiloAdmin.Encabezado("Indicadores y riesgo"));

            this.Shown += (s, e) => Cargar();
        }

        // ── Construcción de controles ───────────────────────────

        private static DataGridView NuevaGrid()
        {
            var g = new DataGridView
            {
                Dock                = DockStyle.Fill,
                ReadOnly            = true,
                MultiSelect         = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloAdmin.EstiloGrid(g);
            return g;
        }

        private static void AgregarColumnas(DataGridView g, params string[] encabezados)
        {
            foreach (string h in encabezados)
            {
                int i = g.Columns.Add(h, h);
                g.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                if (i > 0) g.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private static TabPage CrearPestana(string titulo, DataGridView grid, string explicacion)
        {
            var pagina = new TabPage(titulo) { BackColor = Color.White };
            var nota = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 46,
                Padding   = new Padding(8, 8, 8, 4),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = EstiloAdmin.Gris,
                Text      = explicacion
            };
            pagina.Controls.Add(grid);
            pagina.Controls.Add(nota);
            return pagina;
        }

        // ── Carga de datos ──────────────────────────────────────

        private void Cargar()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                var periodo = cboPeriodo.SelectedItem as Periodo;
                DataTable prov    = _repo.ObtenerIndicadoresProveedores(periodo == null ? null : periodo.Dias);
                DataTable riesgo  = _repo.ObtenerRiesgoOrdenes();
                DataTable tiempos = _repo.ObtenerTiempoPorEstatus();

                LlenarProveedores(prov);
                LlenarRiesgo(riesgo);
                LlenarTiempos(tiempos);
                lblHallazgos.Text = ConstruirHallazgos(prov, riesgo, tiempos);
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudieron cargar los indicadores: " + ex);
                MessageBox.Show("No se pudieron cargar los indicadores.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private static string Num(object valor, string formato)
        {
            return valor == null || valor == DBNull.Value
                ? "—"
                : Convert.ToDecimal(valor).ToString(formato);
        }

        private void LlenarProveedores(DataTable dt)
        {
            dgvProv.Rows.Clear();
            foreach (DataRow r in dt.Rows)
            {
                int idx = dgvProv.Rows.Add(
                    r["proveedor"],
                    r["ordenes"], r["entregadas"], r["a_tiempo"],
                    r["pct_cumplimiento"] == DBNull.Value ? "—" : Num(r["pct_cumplimiento"], "0.#") + " %",
                    Num(r["dias_entrega_prom"], "0.#"),
                    Num(r["retraso_prom_dias"], "0.#"),
                    r["vencidas_abiertas"], r["incidencias"], r["incidencias_abiertas"]);

                // Semáforo del cumplimiento
                if (r["pct_cumplimiento"] != DBNull.Value)
                {
                    decimal pct = Convert.ToDecimal(r["pct_cumplimiento"]);
                    var celda = dgvProv.Rows[idx].Cells["% Cumplimiento"];
                    celda.Style.Font      = new Font("Segoe UI", 9, FontStyle.Bold);
                    celda.Style.ForeColor = pct >= 90 ? Color.FromArgb(22, 163, 74)
                                          : pct >= 70 ? Color.FromArgb(217, 119, 6)
                                          :             Color.FromArgb(220, 38, 38);
                }
            }
        }

        private void LlenarRiesgo(DataTable dt)
        {
            dgvRiesgo.Rows.Clear();
            foreach (DataRow r in dt.Rows)
            {
                string nivel = r["nivel"].ToString();
                int idx = dgvRiesgo.Rows.Add(
                    r["numero_orden"], r["proveedor"], r["estatus"],
                    r["dias_para_vencer"], r["dias_sin_movimiento"], r["incidencias_abiertas"],
                    r["cumplimiento_proveedor"] == DBNull.Value ? "—" : Num(r["cumplimiento_proveedor"], "0.#") + " %",
                    nivel + " (" + r["puntaje"] + ")",
                    r["motivos"]);

                var fila = dgvRiesgo.Rows[idx];
                if (nivel == "Alto")       fila.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                else if (nivel == "Medio") fila.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                fila.Cells["Riesgo"].Style.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }
        }

        private void LlenarTiempos(DataTable dt)
        {
            dgvTiempos.Rows.Clear();
            foreach (DataRow r in dt.Rows)
                dgvTiempos.Rows.Add(r["estatus"], r["estancias"],
                                    Num(r["dias_promedio"], "0.#"), Num(r["dias_maximo"], "0.#"));
        }

        // ── Hallazgos automáticos (reglas simples sobre los mismos datos) ──

        private static string ConstruirHallazgos(DataTable prov, DataTable riesgo, DataTable tiempos)
        {
            var lineas = new List<string>();

            // Proveedores con al menos una entrega realizada
            DataRow mejor = null, peor = null, masRetraso = null;
            int totalEntregas = 0;
            foreach (DataRow r in prov.Rows)
            {
                totalEntregas += Convert.ToInt32(r["entregadas"]);
                if (r["pct_cumplimiento"] == DBNull.Value) continue;

                decimal pct = Convert.ToDecimal(r["pct_cumplimiento"]);
                if (mejor == null || pct > Convert.ToDecimal(mejor["pct_cumplimiento"])) mejor = r;
                if (peor  == null || pct < Convert.ToDecimal(peor["pct_cumplimiento"]))  peor  = r;

                if (r["retraso_prom_dias"] != DBNull.Value &&
                    (masRetraso == null ||
                     Convert.ToDecimal(r["retraso_prom_dias"]) > Convert.ToDecimal(masRetraso["retraso_prom_dias"])))
                    masRetraso = r;
            }

            if (mejor == null)
            {
                lineas.Add("Todavía no hay entregas registradas en este periodo: no se puede calcular el cumplimiento.");
            }
            else
            {
                lineas.Add("Mejor cumplimiento: " + mejor["proveedor"] + " (" + Num(mejor["pct_cumplimiento"], "0.#") +
                           " % de entregas a tiempo, " + mejor["entregadas"] + " entrega(s)).");
                if (!ReferenceEquals(mejor, peor))
                    lineas.Add("Menor cumplimiento: " + peor["proveedor"] + " (" + Num(peor["pct_cumplimiento"], "0.#") +
                               " %, " + peor["entregadas"] + " entrega(s)).");
                if (masRetraso != null)
                    lineas.Add("Mayor retraso promedio cuando se atrasa: " + masRetraso["proveedor"] + " (" +
                               Num(masRetraso["retraso_prom_dias"], "0.#") + " días).");
                if (totalEntregas < 10)
                    lineas.Add("Ojo: hay pocas entregas registradas (" + totalEntregas +
                               "). Los porcentajes son orientativos, no concluyentes.");
            }

            // Riesgo de órdenes abiertas
            int alto = 0, medio = 0;
            foreach (DataRow r in riesgo.Rows)
            {
                string nivel = r["nivel"].ToString();
                if (nivel == "Alto") alto++; else if (nivel == "Medio") medio++;
            }
            if (riesgo.Rows.Count == 0)
            {
                lineas.Add("No hay órdenes abiertas.");
            }
            else
            {
                lineas.Add("Órdenes abiertas: " + riesgo.Rows.Count + " · riesgo alto: " + alto + " · riesgo medio: " + medio + ".");
                if (alto > 0)
                {
                    DataRow top = riesgo.Rows[0];   // ya viene ordenado de mayor a menor riesgo
                    lineas.Add("Mayor riesgo: " + top["numero_orden"] + " (" + top["proveedor"] + ", " +
                               top["puntaje"] + " pts): " + top["motivos"] + ".");
                }
            }

            // Cuello de botella del proceso
            if (tiempos.Rows.Count > 0)
            {
                DataRow lenta = tiempos.Rows[0];    // ya viene ordenado por días promedio
                lineas.Add("Etapa más lenta del proceso: " + lenta["estatus"] + " (" +
                           Num(lenta["dias_promedio"], "0.#") + " días en promedio, " + lenta["estancias"] + " estancia(s)).");
            }
            else
            {
                lineas.Add("Aún no hay suficientes cambios de estatus terminados para medir el tiempo por etapa.");
            }

            return "•  " + string.Join("\n•  ", lineas);
        }
    }
}
