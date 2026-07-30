using SparePartsManager.Data;
using SparePartsManager.Models;
using SqlSugar;

namespace SparePartsManager.Forms;

public partial class FrmStatistics : Form
{
    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private ComboBox cmbDim = null!;
    private Button btnQuery = null!;
    private TabControl tabControl = null!;
    private DataGridView dgvResult = null!;
    private PictureBox picChart = null!;

    private object _chartData = null!;
    private string _chartTitle = "";

    public FrmStatistics()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "统计分析";
        this.BackColor = Color.White;
        this.Padding = new Padding(15);

        var lblTitle = new Label
        {
            Text = "📊 统计分析",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            Dock = DockStyle.Top, Height = 45,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
        };

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top, Height = 55,
            BackColor = Color.FromArgb(245, 245, 250), Padding = new Padding(8, 12, 8, 8)
        };

        pnlTop.Controls.Add(new Label { Text = "从：", Location = new Point(10, 14), AutoSize = true });
        dtpStart = new DateTimePicker { Location = new Point(50, 11), Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddMonths(-1) };
        pnlTop.Controls.Add(dtpStart);
        pnlTop.Controls.Add(new Label { Text = "到：", Location = new Point(180, 14), AutoSize = true });
        dtpEnd = new DateTimePicker { Location = new Point(215, 11), Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Now };
        pnlTop.Controls.Add(dtpEnd);

        pnlTop.Controls.Add(new Label { Text = "维度：", Location = new Point(355, 14), AutoSize = true });
        cmbDim = new ComboBox { Location = new Point(410, 11), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbDim.Items.AddRange(new[] { "出入库概览", "按规格统计", "按人员统计", "按月份趋势" });
        cmbDim.SelectedIndex = 0;
        pnlTop.Controls.Add(cmbDim);

        btnQuery = new Button
        {
            Text = "🔍 查询", Location = new Point(580, 9), Size = new Size(80, 30),
            BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false
        };
        btnQuery.Click += BtnQuery_Click;
        pnlTop.Controls.Add(btnQuery);

        tabControl = new TabControl { Dock = DockStyle.Fill };
        var tabGrid = new TabPage("📋 数据表");
        var tabChart = new TabPage("📈 图表");

        dgvResult = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false
        };
        tabGrid.Controls.Add(dgvResult);

        picChart = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.White };
        picChart.Paint += PicChart_Paint;
        tabChart.Controls.Add(picChart);

        tabControl.TabPages.Add(tabGrid);
        tabControl.TabPages.Add(tabChart);

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlTop);
        this.Controls.Add(lblTitle);
    }

    private void BtnQuery_Click(object? sender, EventArgs e)
    {
        var start = dtpStart.Value.Date;
        var end = dtpEnd.Value.Date.AddDays(1);
        try
        {
            var db = SqlSugarHelper.Db;
            switch (cmbDim.SelectedIndex)
            {
                case 0: QueryOverview(db, start, end); break;
                case 1: QueryBySpec(db, start, end); break;
                case 2: QueryByPerson(db, start, end); break;
                case 3: QueryByMonth(db, start, end); break;
            }
            picChart.Invalidate();
        }
        catch (Exception ex) { MessageBox.Show($"查询失败：{ex.Message}"); }
    }

    private void QueryOverview(ISqlSugarClient db, DateTime s, DateTime e)
    {
        var inC = db.Queryable<SparePart>().Count(p => p.StockInDate >= s && p.StockInDate < e);
        var outC = db.Queryable<SparePart>().Count(p => p.StockOutDate >= s && p.StockOutDate < e);
        var total = db.Queryable<SparePart>().Count(p => p.Status == "InStock");
        _chartTitle = "出入库概览";
        _chartData = new[] { new { 类别 = "入库", 数量 = inC }, new { 类别 = "出库", 数量 = outC }, new { 类别 = "当前库存", 数量 = total } };
        dgvResult.DataSource = _chartData;
    }

    private void QueryBySpec(ISqlSugarClient db, DateTime s, DateTime e)
    {
        var ins = db.Queryable<SparePart>().Where(p => p.StockInDate >= s && p.StockInDate < e).ToList()
            .GroupBy(p => p.Specification).ToDictionary(g => g.Key, g => g.Count());
        var outs = db.Queryable<SparePart>().Where(p => p.StockOutDate >= s && p.StockOutDate < e).ToList()
            .GroupBy(p => p.Specification).ToDictionary(g => g.Key, g => g.Count());
        var allSpecs = ins.Keys.Union(outs.Keys).OrderByDescending(k => ins.GetValueOrDefault(k) + outs.GetValueOrDefault(k));

        var result = allSpecs.Select(k => new { 规格 = k, 入库 = ins.GetValueOrDefault(k, 0), 出库 = outs.GetValueOrDefault(k, 0) }).ToList();
        _chartTitle = "按规格统计";
        _chartData = result;
        dgvResult.DataSource = result;
    }

    private void QueryByPerson(ISqlSugarClient db, DateTime s, DateTime e)
    {
        var inP = db.Queryable<SparePart>().Where(p => p.StockInDate >= s && p.StockInDate < e && !string.IsNullOrEmpty(p.StockInPerson)).ToList()
            .GroupBy(p => p.StockInPerson).ToDictionary(g => g.Key, g => g.Count());
        var outs = db.Queryable<SparePart>().Where(p => p.StockOutDate >= s && p.StockOutDate < e && p.StockOutPerson != null).ToList()
            .GroupBy(p => p.StockOutPerson!).ToDictionary(g => g.Key, g => g.Count());
        var all = inP.Keys.Union(outs.Keys).OrderByDescending(k => inP.GetValueOrDefault(k) + outs.GetValueOrDefault(k));

        var result = all.Select(k => new { 人员 = k, 入库 = inP.GetValueOrDefault(k, 0), 出库 = outs.GetValueOrDefault(k, 0) }).ToList();
        _chartTitle = "按人员统计";
        _chartData = result;
        dgvResult.DataSource = result;
    }

    private void QueryByMonth(ISqlSugarClient db, DateTime s, DateTime e)
    {
        var months = new List<string>();
        var cur = new DateTime(s.Year, s.Month, 1);
        while (cur < e)
        {
            months.Add(cur.ToString("yyyy-MM"));
            cur = cur.AddMonths(1);
        }

        var result = new List<object>();
        foreach (var m in months)
        {
            var ms = DateTime.Parse(m + "-01");
            var me = ms.AddMonths(1);
            var inC = db.Queryable<SparePart>().Count(p => p.StockInDate >= ms && p.StockInDate < me);
            var outC = db.Queryable<SparePart>().Count(p => p.StockOutDate >= ms && p.StockOutDate < me);
            result.Add(new { 月份 = m, 入库 = inC, 出库 = outC });
        }
        _chartTitle = "按月份趋势";
        _chartData = result;
        dgvResult.DataSource = result;
    }

    private void PicChart_Paint(object? sender, PaintEventArgs e)
    {
        if (_chartData == null) return;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var w = picChart.Width;
        var h = picChart.Height;
        g.Clear(Color.White);

        // 动态提取数据
        var dataList = ((System.Collections.IEnumerable)_chartData).Cast<object>().ToList();
        if (dataList.Count == 0) return;

        // 通过反射提取标签和数值
        var props = dataList[0].GetType().GetProperties();
        var labelProp = props.FirstOrDefault(p => p.Name is "类别" or "规格" or "人员" or "月份");
        var valProps = props.Where(p => p.PropertyType == typeof(int)).ToList();
        if (labelProp == null || valProps.Count == 0) return;

        var labels = dataList.Select(d => labelProp.GetValue(d)?.ToString() ?? "").ToList();
        var maxVal = valProps.SelectMany(vp => dataList.Select(d => (int)vp.GetValue(d)!)).Max();
        if (maxVal == 0) maxVal = 1;

        var margin = 60;
        var chartW = w - margin * 2;
        var chartH = h - margin * 2;
        var barCount = dataList.Count * valProps.Count;
        var barW = Math.Max(5, (chartW / barCount) - 4);
        var colors = new[] { Color.FromArgb(0, 122, 204), Color.FromArgb(220, 53, 69), Color.FromArgb(40, 167, 69) };

        g.DrawString(_chartTitle, new Font("Microsoft YaHei", 12, FontStyle.Bold), Brushes.Black, margin, 10);

        int barIdx = 0;
        for (int i = 0; i < dataList.Count; i++)
        {
            for (int j = 0; j < valProps.Count; j++)
            {
                var val = (int)valProps[j].GetValue(dataList[i])!;
                var barH = (int)(val * chartH / maxVal);
                var x = margin + barIdx * (barW + 2);
                var y = h - margin - barH;
                g.FillRectangle(new SolidBrush(colors[j % colors.Length]), x, y, barW, barH);
                if (val > 0 && barIdx % Math.Max(1, barCount / 15) == 0)
                {
                    g.DrawString(val.ToString(), new Font("Arial", 7), Brushes.Black, x, y - 14);
                }
                barIdx++;
            }
        }

        // X轴标签
        if (dataList.Count <= 20)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                var x = margin + i * valProps.Count * (barW + 2);
                g.DrawString(labels[i], new Font("Arial", 7), Brushes.Gray,
                    x, h - margin + 2, new StringFormat { Alignment = StringAlignment.Near });
            }
        }

        // 轴
        g.DrawLine(Pens.Gray, margin, h - margin, w - margin, h - margin);
        g.DrawLine(Pens.Gray, margin, margin, margin, h - margin);

        // 图例
        for (int j = 0; j < valProps.Count; j++)
        {
            g.FillRectangle(new SolidBrush(colors[j % colors.Length]), w - 180, 12 + j * 18, 12, 12);
            g.DrawString(valProps[j].Name, new Font("Microsoft YaHei", 8), Brushes.Black, w - 164, 11 + j * 18);
        }
    }
}
