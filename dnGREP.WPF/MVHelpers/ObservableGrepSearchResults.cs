﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using dnGREP.Common;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;
using System.Collections.Specialized;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Runtime.InteropServices;
using System.ComponentModel;
using dnGREP.Common.UI;

namespace dnGREP.WPF
{
	public class ObservableGrepSearchResults : ObservableCollection<FormattedGrepResult>
	{
        private string folderPath = "";

		public string FolderPath
		{
			get { return folderPath; }
			set { folderPath = value; }
		}


		public ObservableGrepSearchResults()
		{
            this.CollectionChanged += new NotifyCollectionChangedEventHandler(ObservableGrepSearchResults_CollectionChanged);
        }

        //protected override void ClearItems()
        //{
        //    base.ClearItems();
        //    OnFunctionCalled("Clear");
        //}

        private Dictionary<string, BitmapSource> icons = new Dictionary<string, BitmapSource>(); 

        void ObservableGrepSearchResults_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (FormattedGrepResult newEntry in e.NewItems.Cast<FormattedGrepResult>())
                {
                    string extension = Path.GetExtension(newEntry.GrepResult.FileNameDisplayed);
                    if (extension.Length <= 1)
                        extension = ".na";
                    if (!icons.ContainsKey(extension))
                    {
                        System.Drawing.Bitmap bitmapIcon = IconHandler.IconFromExtensionShell(extension, IconSize.Small);
                        if (bitmapIcon == null)
                            bitmapIcon = dnGREP.Common.Properties.Resources.na_icon;
                        icons[extension] = GetBitmapSource(bitmapIcon);
                    }
                    newEntry.Icon = icons[extension];
                }
            }
        }

		public ObservableGrepSearchResults(List<GrepSearchResult> list) : this()
		{
			AddRange(list);
		}

		public List<GrepSearchResult> GetList()
		{
			List<GrepSearchResult> tempList = new List<GrepSearchResult>();
			foreach (var l in this) tempList.Add(l.GrepResult);
			return tempList;
		}

		public void AddRange(List<GrepSearchResult> list)
		{
			foreach (var l in list) this.Add(new FormattedGrepResult(l, folderPath));
		}

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);
        public static BitmapSource GetBitmapSource(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            try
            {
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                return bs;
            }
            finally
            {
                DeleteObject(ip);
            }
        }

        //#region PropertyChanged Members
        //// Create the OnPropertyChanged method to raise the event
        //protected void OnFunctionCalled(string name)
        //{
        //    FunctionCallEventHandler handler = FunctionCalled;
        //    if (handler != null)
        //    {
        //        handler(this, new PropertyChangedEventArgs(name));
        //    }
        //}

        //public event FunctionCallEventHandler FunctionCalled;
        //public delegate void FunctionCallEventHandler(object sender, PropertyChangedEventArgs e);

        //#endregion
	}

    public class FormattedGrepResult : INotifyPropertyChanged
	{
		private GrepSearchResult grepResult = new GrepSearchResult();
		public GrepSearchResult GrepResult
		{
			get { return grepResult; }
		}

		private string style = "";
		public string Style
		{
			get { return style; }
			set { style = value; }
		}

		private string label = "";
		public string Label
		{
			get
			{
				return label;
			}
		}

        private bool isExpanded = false;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { isExpanded = value; OnPropertyChanged("IsExpanded"); }
        }

		private int lineNumberColumnWidth = 30;
		public int LineNumberColumnWidth
		{
			get { return lineNumberColumnWidth; }
			set { lineNumberColumnWidth = value; OnPropertyChanged("LineNumberColumnWidth"); }
		}

        private BitmapSource icon;

        public BitmapSource Icon
        {
            get { return icon; }
            set { icon = value; }
        }

		private List<FormattedGrepLine> formattedLines = new List<FormattedGrepLine>();
		public List<FormattedGrepLine> FormattedLines
		{
			get { return formattedLines; }
		}

		public FormattedGrepResult(GrepSearchResult result, string folderPath)
		{
			grepResult = result;

			if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ExpandResults))
			{
				IsExpanded = true;
			}

			bool isFileReadOnly = Utils.IsReadOnly(grepResult);
            bool isSuccess = grepResult.IsSuccess;
			string displayedName = Path.GetFileName(grepResult.FileNameDisplayed);
			if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFilePathInResults) &&
				grepResult.FileNameDisplayed.Contains(Utils.GetBaseFolder(folderPath) + "\\"))
			{
				displayedName = grepResult.FileNameDisplayed.Substring(Utils.GetBaseFolder(folderPath).Length + 1);
			}
			int lineCount = Utils.MatchCount(grepResult);
			if (lineCount > 0)
				displayedName = string.Format("{0} ({1})", displayedName, lineCount);
            if (isFileReadOnly)
            {
                result.ReadOnly = true;
                displayedName = displayedName + " [read-only]";
            }

			label = displayedName;

			if (isFileReadOnly)
			{
				style = "ReadOnly";
			}
            if (!isSuccess)
            {
                style = "Error";
            }

			if (result.SearchResults != null)
			{
				int currentLine = -1;
				for (int i = 0; i < result.SearchResults.Count; i++)
				{
					GrepSearchResult.GrepLine line = result.SearchResults[i];
					
					currentLine = line.LineNumber;
					if (currentLine <= 999 && LineNumberColumnWidth < 30)
						LineNumberColumnWidth = 30;
					else if (currentLine > 999 && LineNumberColumnWidth < 35)
						LineNumberColumnWidth = 35;
					else if (currentLine > 9999 && LineNumberColumnWidth < 47)
						LineNumberColumnWidth = 47;
					else if (currentLine > 99999 && LineNumberColumnWidth < 50)
						LineNumberColumnWidth = 50;
					formattedLines.Add(new FormattedGrepLine(line, this, LineNumberColumnWidth));
				}
			}
		}

        #region PropertyChanged Members
        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
	}

    public class FormattedGrepLine : INotifyPropertyChanged
	{
		private GrepSearchResult.GrepLine grepLine;
		public GrepSearchResult.GrepLine GrepLine
		{
			get { return grepLine; }
		}

        private string formattedLineNumber;
        public string FormattedLineNumber
        {
            get { return formattedLineNumber; }
        }

		private InlineCollection formattedText;
		public InlineCollection FormattedText
		{
			get {
                if (formattedText == null || formattedText.Count == 0)
                    formattedText = formatLine(GrepLine);
                return formattedText; 
            }
		}

        private SyntaxHighlighterViewModel previewViewModel;

        public SyntaxHighlighterViewModel PreviewViewModel
        {
            get {
                if (previewViewModel == null)
                    previewViewModel = new SyntaxHighlighterViewModel();
                previewViewModel.Text = GrepLine.LineText;
                previewViewModel.LineNumber = GrepLine.LineNumber;
                return previewViewModel; 
            }
        }


		private string style = "";
		public string Style
		{
			get { return style; }
			set { style = value; }
		}

		private int lineNumberColumnWidth = 30;
		public int LineNumberColumnWidth
		{
			get { return lineNumberColumnWidth; }
			set { lineNumberColumnWidth = value; OnPropertyChanged("LineNumberColumnWidth"); }
		}

		void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "LineNumberColumnWidth")
				LineNumberColumnWidth = Parent.LineNumberColumnWidth;
		}

        private FormattedGrepResult parent;
        public FormattedGrepResult Parent
        {
            get { return parent; }
            set { parent = value; }
        }

		public FormattedGrepLine(GrepSearchResult.GrepLine line, FormattedGrepResult parent, int initialColumnWidth)
		{
            Parent = parent;
			grepLine = line;
			Parent.PropertyChanged += new PropertyChangedEventHandler(Parent_PropertyChanged);
			LineNumberColumnWidth = initialColumnWidth;
                        
            formattedLineNumber = (line.LineNumber == -1 ? "" : line.LineNumber.ToString());

			//string fullText = lineSummary;
			if (line.IsContext)
			{
				style = "Context";
			}
			if (line.LineNumber == -1 && line.LineText == "")
			{
				style = "Empty";
			}
        }

        private InlineCollection formatLine(GrepSearchResult.GrepLine line)
        {
            Paragraph paragraph = new Paragraph();
            var font = new FontFamily("Consolas");
 
            if (line.Matches.Count == 0)
            {
                Run mainRun = new Run(line.LineText);
                paragraph.Inlines.Add(mainRun);
            }
            else
            {
                int counter = 0;
                string fullLine = line.LineText;
				GrepSearchResult.GrepMatch[] lineMatches = new GrepSearchResult.GrepMatch[line.Matches.Count];
				line.Matches.CopyTo(lineMatches);
				foreach (GrepSearchResult.GrepMatch m in lineMatches)
                {
                    try
                    {
                        string regLine = null;
                        string fmtLine = null;
                        if (fullLine.Length < m.StartLocation + m.Length)
                        {
                            regLine = fullLine;
                        }
                        else
                        {
                            regLine = fullLine.Substring(counter, m.StartLocation - counter);
                            fmtLine = fullLine.Substring(m.StartLocation, m.Length);
                        }

                        Run regularRun = new Run(regLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);

                        if (fmtLine != null)
                        {
                            Run highlightedRun = new Run(fmtLine);
                            highlightedRun.FontFamily = font;
                            highlightedRun.Background = Brushes.Yellow;
                            paragraph.Inlines.Add(highlightedRun);
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Run regularRun = new Run(fullLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);
                    }
                    finally
                    {
                        counter = m.StartLocation + m.Length;
                    }
                }
                if (counter < fullLine.Length)
                {
                    try
                    {
                        string regLine = fullLine.Substring(counter);
                        Run regularRun = new Run(regLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);
                    }
                    catch (Exception e)
                    {
                        Run regularRun = new Run(fullLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);
                    }
                }
            }
            return paragraph.Inlines;
        }

        #region PropertyChanged Members
        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}