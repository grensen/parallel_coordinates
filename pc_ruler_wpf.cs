using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data;

public class TheWindow : Window
{
    // colors
    readonly SolidColorBrush font = new(Color.FromRgb(250, 240, 230));
    // support 
    readonly System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.GetCultureInfo("en-us");
    readonly Typeface tf = new("TimesNewRoman"); // Arial // TimesNewRoman

    // layout
    Canvas canGlobal = new Canvas(), canVisual = new(), canCurrent = new(), canRuler = new(), canTest= new();

    double fs = 0; // feature size
    double height = 0;

    int dataStateCnt = 0, styleCnt = 0;
    int ggCount = 0;
    int features = 0, labelNum = 0;
    int ys = 40; // y start point
    int xs = 20; // x start point menu
    float[] minVal = new float[49], maxVal = new float[49];
    float minAll = float.MaxValue, maxAll = float.MinValue;

    bool[] featureState = new bool[49];
    bool[] isLabel = new bool[10];
    float[] trainData;
    int[] trainLabels;
    float[] testData;
    int[] testLabels;

    Brush[] br = new Brush[10];
    string name = "";
    string[] featureNames = null;
    string classesInfo = "";

    bool clicked = false;
    int lastPoint = 0;
    int userY = 0;
    int featureRule = -1;
    int labelRuler = 0;
    int secondWindow = 420;

    List<Rules> rules = new List<Rules>();

    [STAThread]
    public static void Main() { new Application().Run(new TheWindow()); }

    // CONSTRUCTOR - LOADED - ONINIT
    TheWindow() // constructor // set window   
    {
        Content = canGlobal;
        Title = "Parallel coordinates 23";
        Background = RGB(0, 0, 0);
        Width = 960;
        Height = 540 + secondWindow;

        SizeChanged += Window_SizeChanged;
        MouseDown += Mouse_Down;
        MouseMove += Mouse_Move;
        MouseUp += Mouse_Up;

        canGlobal.Children.Add(canVisual);
        canGlobal.Children.Add(canRuler);
        canGlobal.Children.Add(canCurrent);
        canGlobal.Children.Add(canTest);
        IrisDataInit(); ColorInit();
        return;
        // continue in Window_SizeChanged()...
    } // TheWindow end

    void Mouse_Up(object sender, MouseEventArgs e)
    {
        clicked = false; lastPoint = -1;
        int gpy = (int)e.GetPosition(this).Y, gpx = (int)e.GetPosition(this).X;
        if (featureRule == -1) return;

        DrawingContext dc = ContextHelpMod(true, ref canRuler);
        if (0 < userY - gpy) // min to max - down to up
        {
            int y = gpy;
            if (gpy < ys) y = ys;
            var max_rule_val = maxVal[featureRule] + ((y - ys) / height) * (minVal[featureRule] - maxVal[featureRule]);
            var min_rule_val = maxVal[featureRule] + ((userY - ys) / height) * (minVal[featureRule] - maxVal[featureRule]);
            rules.Add(new Rules { max = max_rule_val, min = min_rule_val, feature = featureRule, label = labelRuler });

            dc.DrawRectangle(br[labelRuler], null, new Rect((featureRule + 1) * fs, y, 25, userY - y));
            Text(ref dc, "#" + (rules.Count).ToString(), 9, font, (int)((featureRule + 1) * fs) + 2, y + 2);
            Text(ref dc, "↑" + (max_rule_val).ToString("F2"), 9, font, (int)((featureRule + 1) * fs) + 2, y + 12);
            Text(ref dc, "↓" + (min_rule_val).ToString("F2"), 9, font, (int)((featureRule + 1) * fs) + 2, y + 22);
            Text(ref dc, "" + (labelRuler).ToString(), 9, font, (int)((featureRule + 1) * fs) + 2, y + 32);
        }
        if (0 > userY - gpy)// max to min - up to down
        {
            int y = gpy;
            if (gpy > ys + height) y = (int)(ys + height);
            var max_rule_val = maxVal[featureRule] + ((userY - ys) / height) * (minVal[featureRule] - maxVal[featureRule]);
            var min_rule_val = maxVal[featureRule] + ((y - ys) / height) * (minVal[featureRule] - maxVal[featureRule]);
            rules.Add(new Rules { max = max_rule_val, min = min_rule_val, feature = featureRule, label = labelRuler });

            dc.DrawRectangle(br[labelRuler], null, new Rect((featureRule + 1) * fs, userY, 25, y - userY));
            Text(ref dc, "#" + (rules.Count).ToString(), 9, font, (int)((featureRule + 1) * fs) + 2, userY + 2);
            Text(ref dc, "↑" + (max_rule_val).ToString("F2"), 9, font, (int)((featureRule + 1) * fs) + 2, userY + 12);
            Text(ref dc, "↓" + (min_rule_val).ToString("F2"), 9, font, (int)((featureRule + 1) * fs) + 2, userY + 22);
            Text(ref dc, "" + (labelRuler).ToString(), 9, font, (int)((featureRule + 1) * fs) + 2, userY + 32);
        }

        featureRule = -1;
        dc.Close();
        canCurrent.Children.Clear();   // clear move canvas
    }

    void Mouse_Move(object sender, MouseEventArgs e)
    {
        int gpy = (int)e.GetPosition(this).Y, gpx = (int)e.GetPosition(this).X;
        int currentPoint = gpy * (int)((Canvas)this.Content).RenderSize.Width + gpx;

        if (clicked && currentPoint != lastPoint)
        {
            DrawingContext dc = ContextHelpMod(false, ref canCurrent);

            for (int j = 0; j < features; j++)
            {
                if (gpx > (j + 1) * fs - 15 && gpx < (j + 1) * fs + 25 + 15)
                {
                    featureRule = j;
                    if (0 < userY - gpy)
                    {
                        double y = gpy;
                        if (gpy < ys) y = ys;
                        dc.DrawRectangle(br[labelRuler], null, new Rect((j + 1) * fs, y, 25, userY - y));
                    }
                    if (0 > userY - gpy)
                    {
                        double y = gpy;
                        if (gpy > ys + height) y = ys + height;
                        dc.DrawRectangle(br[labelRuler], null, new Rect((j + 1) * fs, userY, 25, y - userY));
                    }
                    break;
                }
            }
            dc.Close();
        }
        lastPoint = currentPoint;
    }

    void Mouse_Down(object sender, MouseButtonEventArgs e)
    {
        int gpy = (int)e.GetPosition(this).Y, gpx = (int)e.GetPosition(this).X;

        // ruler visual
        if (gpx > xs && gpx < xs + 80 && gpy > ys + height + 30 && gpy < ys + height + 30 + 20)
        {
            IrisTest(); return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            // check inside pc bounds, then check which feature is clicked
            if (gpx > xs && gpx < xs + features * fs && gpy > ys && gpy < ys + height)
            {
                for (int j = 0; j < features; j++)
                    if (gpx > (j + 1) * fs - 15 && gpx < (j + 1) * fs + 25 + 15)
                    {
                        featureRule = j;
                        clicked = true;
                        userY = gpy; return;
                    }
            }
        }
        else if (e.ChangedButton == MouseButton.Right) // clear your rules
        {
            canRuler.Children.Clear();
            canTest.Children.Clear();
            rules.Clear();
            DrawingContext dc = ContextHelpMod(true, ref canRuler);
            dc.Close();
            canCurrent.Children.Clear(); return;
        }

        ButtonActions();

        void ButtonActions()
        {
            // data button
            if (gpy > ys + 0 && gpy < ys + 0 + 20 && gpx > xs && gpx < xs + 20)
            {
                dataStateCnt++;
                IrisDataInit();
                DrawParallelCoordinates();
            }
            // class on off button
            for (int i = 0; i < labelNum; i++)
                if (gpy > ys + 30 + i * 30 && gpy < ys + 30 + i * 30 + 20 && gpx > xs && gpx < xs + 20)
                {
                    isLabel[i] = !isLabel[i];
                    DrawParallelCoordinates(); break;
                }
            // set label for ruler button
            if (gpy > ys + labelNum * 30 + 30 && gpy < ys + labelNum * 30 + 30 + 20 && gpx > xs && gpx < xs + 20)
            {
                labelRuler++;
                if (labelRuler > labelNum - 1)
                {
                    labelRuler = 0;
                }
                DrawParallelCoordinates();
            }
            // style parallel coordinates button
            if (gpy > ys + labelNum * 30 + 60 && gpy < ys + labelNum * 30 + 60 + 20 && gpx > xs && gpx < xs + 20)
            {
                styleCnt++;
                DrawParallelCoordinates();
            }
            // shuffle data button
            if (gpy > ys + labelNum * 30 + 90 && gpy < ys + labelNum * 30 + 90 + 20 && gpx > xs && gpx < xs + 20)
            {
                DrawParallelCoordinates(true); // shuffle data
            }

            // normalize, a lot todo
            if (gpy > ys + labelNum * 30 + 120 && gpy < ys + labelNum * 30 + 120 + 20 && gpx > xs && gpx < xs + 20)
            {
                for (int i = 0; i < features; i++)
                {
                    if (minVal[i] < minAll) { minAll = minVal[i]; }
                    if (maxVal[i] > maxAll) { maxAll = maxVal[i]; }
                }
                // check min and max for all features
                for (int i = 0; i < features; i++) minVal[i] = minAll;
                for (int i = 0; i < features; i++) maxVal[i] = maxAll;

                DrawParallelCoordinates(); 
            }
            // on off features bottom
            for (int i = 0; i < features; i++)
                if (gpy > ys + height + 5 && gpy < ys + height + 5 + 20 && gpx > xs + i * fs && gpx < xs + i * fs + 20)
                {
                    featureState[i] = !featureState[i];
                    DrawParallelCoordinates(); break;
                }
        }
    }
    void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetWindow();
        DrawParallelCoordinates();
    } // Window_SizeChanged end

    void DrawParallelCoordinates(bool dataShuffle = false)
    {
        int len = trainLabels.Length >= 5000 ? 5000 * features : trainData.Length;
        DrawingContext dc = ContextHelpMod(false, ref canVisual);
        BackgroundStuff(ref dc, features, len);
        int[] ids = new int[trainLabels.Length];
        Shuffle(ids, dataShuffle, new Random(ggCount++));
        styleCnt = styleCnt > 2 ? 0 : styleCnt;

        double yh = ys + height;
        float[] maxMinusMin = new float[maxVal.Length];
        for (int i = 0; i < maxMinusMin.Length; i++)
            maxMinusMin[i] = 1.0f / (maxVal[i] - minVal[i]);

        double lineHeight = height / (labelNum - 1);
        
        for (int i = 0, lb = 0; i < len; i += features, lb++)
        {
            int lab = trainLabels[ids[lb]];
            if (!isLabel[lab]) continue;
            int id = ids[lb] * features;
            Brush clr = br[lab];
            double startLine = ys + lab * lineHeight;
            Pen pen = new(clr, 0.5);

            if (styleCnt == 0)
                for (int j = 0; j < features; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = yh - height * (trainData[id + j] - minVal[j]) * maxMinusMin[j];
                    Line2(dc, pen, 15 + j * fs, startLine, 15 + (j + 1) * fs, pixelPos);
                    startLine = pixelPos;
                }
            else if (styleCnt == 1)
                for (int j = 0; j < features; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = yh - height * (trainData[id + j] - minVal[j]) * maxMinusMin[j];
                    Line2(dc, pen, 15 + j * fs, startLine, 15 + (j + 1) * fs, pixelPos);
                }
            else if (styleCnt == 2)
                for (int j = 0; j < features; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = yh - height * (trainData[id + j] - minVal[j]) * maxMinusMin[j];
                    Line2(dc, pen, 15 + j * fs, pixelPos, 15 + (j + 1) * fs, pixelPos);
                }
        }
        
        // ruler visual
        Rect(ref dc, RGB(35, 84, 84), xs, (int)(ys + height + 30), 80, 20); // Style
        Text(ref dc, "Test rules!", 10, font, xs + 10, (int)(ys + height + 35));


        // values max min visual plus lines  
        for (int j = 0; j < features; j++)
        {
            dc.DrawLine(new Pen(font, 1.0), new Point(15 + (j + 1) * fs, ys), new Point(15 + (j + 1) * fs, ys + height));
            var min = minVal[j];
            double range = maxVal[j] - min;
            for (int i = 0, cats = 20; i < cats + 1; i++) // accuracy lines 0, 20, 40...
            {
                double yGrph = ys + height - i * (height / cats);
                Line(ref dc, font, 1.0, 15 + (j + 1) * fs - 5, yGrph, 15 + (j + 1) * fs, yGrph);
                Text(ref dc, (range / cats * i + min).ToString("F2"), 8, font, (int)((j + 1) * fs - 10), (int)yGrph - 5);
            }
        }

        InfoStuff(ref dc, features, ys);
        dc.Close();

        void InfoStuff(ref DrawingContext dc, int inputLen, int ys)
        {
            // dirty 
            int ys2 = ys;
            Text(ref dc, "Data", 10, font, xs + 30, ys + 5); 
            Rect(ref dc, RGB(64, 64, 64), xs, ys + 0, 20, 20);// switch data button visual

            for (int i = 0; i < labelNum; i++, ys += 30)
            {
                string str = "Class " + i.ToString();
                Rect(ref dc, RGB(129, 129, 129), xs + 30, ys + 35, 32, 12);
                Text(ref dc, str, 10, br[i], xs + 30, ys + 35);

                Rect(ref dc, RGB(64, 64, 64), xs - 1, ys + 29, 22, 22); // background class button
                Rect(ref dc, (isLabel[i] ? br[i] : RGB(64, 64, 64)), xs, ys + 30, 20, 20); // class button
            }

            Text(ref dc, "Ruler", 10, font, xs + 30, ys + 35);
            Text(ref dc, "Style", 10, font, xs + 30, ys + 65);
            Text(ref dc, "Shuffle", 10, font, xs + 30, ys + 95);
            Text(ref dc, "Normalize", 10, font, xs + 30, ys + 125);

            Rect(ref dc, labelRuler < 0 ? RGB(64, 64, 64) : br[labelRuler], xs, ys + 30, 20, 20); // Ruler
            Rect(ref dc, RGB(235, 184, 184), xs, ys + 60, 20, 20); // Style
            Rect(ref dc, RGB(184, 235, 184), xs, ys + 90, 20, 20); // Shuffle
            Rect(ref dc, RGB(184, 184, 235), xs, ys + 120, 20, 20); // Normalize

            for (int i = 0; i < featureState.Length; i++)
                Rect(ref dc, featureState[i] ? RGB(255, 255, 255) : RGB(16, 16, 16), (int)(xs + i * fs), (int)(ys2 + height + 5), 20, 20);
        }
        void BackgroundStuff(ref DrawingContext dc, int inputLen, int len)
        {
            string str = ", to predict " + classesInfo;
            Text(ref dc, name + " dataset with " + (len / inputLen).ToString() + " examples, " + inputLen.ToString()
                + " features and " + labelNum.ToString() + " labels"
                + str, 12, font, (int)(15 + 0 * fs), 5);

            Rect(ref dc, RGB(24, 24, 24), 15, ys, (int)(inputLen * fs), (int)height);

            // feature id's
            for (int i = 0; i < inputLen; i++)
                Text(ref dc, i.ToString() + " = " + featureNames[i], 7, Brushes.White, (int)(15 + i * fs), 30);           
        }
        void Line(ref DrawingContext dc, Brush rgb, double size, double xl, double yl, double xr, double yr) => dc.DrawLine(new Pen(rgb, size), new Point(xl, yl), new Point(xr, yr));
        void Line2(DrawingContext dc, Pen pen, double xl, double yl, double xr, double yr) => dc.DrawLine(pen, new Point(xl, yl), new Point(xr, yr));  
    }
    void Text(ref DrawingContext dc, string str, int size, Brush rgb, int x, int y) => dc.DrawText(new FormattedText(str, ci, FlowDirection.LeftToRight, tf, size, rgb, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));

    void Rect(ref DrawingContext dc, Brush rgb, int x, int y, int width, int height) => dc.DrawRectangle(rgb, null, new Rect(x, y, width, height));

    static void Shuffle(int[] route, bool shuffle, Random rn)
    {
        int n = route.Length;
        for (int i = 0; i < n; i++) route[i] = i;

        if (shuffle)
            for (int i = 0, r = rn.Next(0, n); i < n; i++, r = rn.Next(i, n))
                (route[r], route[i]) = (route[i], route[r]);
    }
    void IrisTest()
    {
        DrawingContext dc = ContextHelpMod(false, ref canTest);
        Text(ref dc, "Test dataset with " + (testLabels.Length).ToString() + " examples, " + (testData.Length / testLabels.Length).ToString()
        + " features and " + (labelNum).ToString() + " labels"
            + "", 12, font, (int)(15 + 0 * fs), (int)(ys + height + 55));
        // test rules
        int c = 0, unsure = 0;
        for (int i = 0; i < testLabels.Length; i++)
        {
            int prediction = -1, rule_id = 0;
            foreach (var rule in rules)
            {
                // check if rule is touched
                var val = testData[i * 4 + rule.feature];
                if (val > rule.min && val < rule.max)
                {
                    prediction = rule.label;
                    Text(ref dc, "Id = " + i.ToString() + ", prediction = " + prediction.ToString()
                        + ", min = " + rule.min.ToString("F2")
                        + ", max = " + rule.max.ToString("F2")
                        + ", val = " + val.ToString("F2")
                        + ", rule # " + (rule_id + 1).ToString("F0")
                        , 10, font, 15, (int)(ys + height + 95 + i * 11));
                    break;
                }
                rule_id++;
            }

            if (prediction == -1)
            {
                Text(ref dc, "Id = " + i.ToString() + ", no prediction!!! ", 10, font, 15, (int)(ys + height + 95 + i * 11));
                unsure++;
            }
            else if (prediction == testLabels[i]) c++;
        }
        Text(ref dc, "iris_test accuracy = " + (c / (double)testLabels.Length).ToString("F2"), 12, font, 15 + 0, (int)(ys + height + 75));
        dc.Close();
    }

    void IrisDataInit()
    {
        name = "iris_train";
        labelNum = 3;
        features = 4;

        var trainPath = @"C:\datasets\iris_train.txt";
        trainData = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).SelectMany(values => values.Skip(0).Take(4).Select(float.Parse)).ToArray();
        trainLabels = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).Select(values => int.Parse(values.Last())).ToArray();

        string attributes = "sepal length, sepal width, petal length, petal width";
        featureNames = attributes.Substring(0).Split(',');
        classesInfo = "Species: 0 = setosa, 1 = versicolor, 2 = virginica";

        var testPath = @"C:\datasets\iris_test.txt";
        testData = File.ReadAllLines(testPath).Skip(2).Select(line => line.Split(',')).SelectMany(values => values.Skip(0).Take(4).Select(float.Parse)).ToArray();
        testLabels = File.ReadAllLines(testPath).Skip(2).Select(line => line.Split(',')).Select(values => int.Parse(values.Last())).ToArray();

        minVal = new float[features];
        maxVal = new float[features];
        featureState = new bool[features];
        for (int vals = 0; vals < features; vals++)
        {
            float cmin = float.MaxValue, cmax = float.MinValue;
            for (int dt = 0; dt < trainLabels.Length; dt++)
            {
                var val = trainData[vals + features * dt];
                if (val < cmin) { cmin = val; }
                if (val > cmax) { cmax = val; }
            }
            minVal[vals] = cmin; maxVal[vals] = cmax;
        }

        for (int i = 0; i < features; i++) featureState[i] = true;
        isLabel = new bool[10];
        for (int i = 0; i < 10; i++) isLabel[i] = true;

        SetWindow();
    }
    void SetWindow()
    {
        fs = (((Canvas)this.Content).RenderSize.Width - 30) / features;
        height = ((Canvas)this.Content).RenderSize.Height - secondWindow - 60;
    }

    static DrawingContext ContextHelpMod(bool isInit, ref Canvas cTmp)
    {
        if (!isInit) cTmp.Children.Clear();
        DrawingVisualElement drawingVisual = new();
        cTmp.Children.Add(drawingVisual);
        return drawingVisual.drawingVisual.RenderOpen();
    }
    void ColorInit()
    {
        br[0] = RGB(50, 70, 255); // blue
        br[1] = RGB(225, 168, 0); // gold
        br[2] = RGB(255, 0, 0); // red
        br[3] = RGB(161, 195, 255); // baby blue                  
        br[4] = RGB(0, 255, 0); // green
        br[5] = RGB(255, 0, 255); // magenta
        br[6] = RGB(75, 0, 130); // indigo
        br[7] = RGB(0, 128, 128); // teal
        br[8] = RGB(128, 128, 70); // olive
        br[9] = RGB(255, 222, 0); // yellow
    }
    static Brush RGB(byte red, byte green, byte blue)
    {
        Brush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
} // TheWindow end

struct Rules
{
    public double max { get; set; }
    public double min { get; set; }
    public int feature { get; set; }
    public int label { get; set; }
}

public class DrawingVisualElement : FrameworkElement
{
    private readonly VisualCollection _children;
    public DrawingVisual drawingVisual;

    public DrawingVisualElement()
    {
        _children = new VisualCollection(this);
        drawingVisual = new DrawingVisual();
        _children.Add(drawingVisual);
    }

    public void ClearVisualElement()
    {
        _children.Clear();
    }

    protected override int VisualChildrenCount => _children.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }
}

