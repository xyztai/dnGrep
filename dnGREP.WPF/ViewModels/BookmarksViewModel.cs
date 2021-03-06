﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Everything;

namespace dnGREP.WPF
{
    public class BookmarkListViewModel : ViewModelBase
    {
        readonly Action<string, string, string> ClearStar;
        readonly Window ownerWnd;

        public BookmarkListViewModel(Window owner, Action<string, string, string> clearStar)
        {
            ownerWnd = owner;
            ClearStar = clearStar;

            var items = BookmarkLibrary.Instance.Bookmarks.Select(bk => new BookmarkViewModel(bk)).ToList();
            Bookmarks = new ListCollectionView(items)
            {
                Filter = BookmarkFilter
            };
        }

        public ListCollectionView Bookmarks { get; private set; }

        private bool BookmarkFilter(object obj)
        {
            if (obj is BookmarkViewModel bmk)
            {
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    return true;
                }
                else
                {
                    return bmk.Description.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           bmk.SearchFor.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           bmk.ReplaceWith.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           bmk.FilePattern.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            return false;
        }

        private string filterText = string.Empty;
        public string FilterText
        {
            get { return filterText; }
            set
            {
                if (filterText == value)
                    return;

                filterText = value;
                OnPropertyChanged("FilterText");

                Bookmarks.Refresh();
            }
        }

        private BookmarkViewModel selectedBookmark = null;
        public BookmarkViewModel SelectedBookmark
        {
            get { return selectedBookmark; }
            set
            {
                if (selectedBookmark == value)
                    return;

                selectedBookmark = value;
                OnPropertyChanged("SelectedBookmark");

                HasSelection = selectedBookmark != null;
            }
        }

        private bool hasSelection = false;
        public bool HasSelection
        {
            get { return hasSelection; }
            set
            {
                if (hasSelection == value)
                    return;

                hasSelection = value;
                OnPropertyChanged("HasSelection");
            }
        }


        RelayCommand _addCommand;
        public ICommand AddCommand
        {
            get
            {
                if (_addCommand == null)
                {
                    _addCommand = new RelayCommand(
                        param => AddBookmark()
                        );
                }
                return _addCommand;
            }
        }

        RelayCommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (_editCommand == null)
                {
                    _editCommand = new RelayCommand(
                        param => Edit(),
                        param => SelectedBookmark != null
                        );
                }
                return _editCommand;
            }
        }

        RelayCommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                if (_deleteCommand == null)
                {
                    _deleteCommand = new RelayCommand(
                        param => Delete(),
                        param => SelectedBookmark != null
                        );
                }
                return _deleteCommand;
            }
        }

        private void Delete()
        {
            if (SelectedBookmark != null)
            {
                ClearStar(SelectedBookmark.SearchFor, SelectedBookmark.ReplaceWith, SelectedBookmark.FilePattern);

                var bmk = BookmarkLibrary.Instance.Find(SelectedBookmark.SearchFor, SelectedBookmark.ReplaceWith, SelectedBookmark.FilePattern);
                if (bmk != null)
                {
                    BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                    BookmarkLibrary.Save();

                    Bookmarks.Remove(SelectedBookmark);
                }
            }
        }

        private void Edit()
        {
            if (SelectedBookmark != null)
            {
                // edit a copy
                var editBmk = new BookmarkViewModel(SelectedBookmark);
                var dlg = new BookmarkDetailWindow
                {
                    DataContext = editBmk,
                    Owner = ownerWnd
                };

                var result = dlg.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    if (SelectedBookmark.SearchFor != editBmk.SearchFor ||
                        SelectedBookmark.ReplaceWith != editBmk.ReplaceWith ||
                        SelectedBookmark.FilePattern != editBmk.FilePattern)
                    {
                        ClearStar(SelectedBookmark.SearchFor, SelectedBookmark.ReplaceWith, SelectedBookmark.FilePattern);
                    }

                    var bmk = BookmarkLibrary.Instance.Find(SelectedBookmark.SearchFor, SelectedBookmark.ReplaceWith, SelectedBookmark.FilePattern);
                    if (bmk != null)
                    {
                        BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                        Bookmarks.Remove(SelectedBookmark);
                    }

                    var newBmk = new Bookmark(editBmk.SearchFor, editBmk.ReplaceWith, editBmk.FilePattern)
                    {
                        Description = editBmk.Description,
                        IgnoreFilePattern = editBmk.IgnoreFilePattern,
                        TypeOfFileSearch = editBmk.TypeOfFileSearch,
                        TypeOfSearch = editBmk.TypeOfSearch,
                        CaseSensitive = editBmk.CaseSensitive,
                        WholeWord = editBmk.WholeWord,
                        Multiline = editBmk.Multiline,
                        Singleline = editBmk.Singleline,
                        BooleanOperators = editBmk.BooleanOperators,
                        IncludeSubfolders = editBmk.IncludeSubfolders,
                        MaxSubfolderDepth = editBmk.MaxSubfolderDepth,
                        IncludeHiddenFiles = editBmk.IncludeHidden,
                        IncludeBinaryFiles = editBmk.IncludeBinary,
                        UseGitignore = editBmk.UseGitignore,
                        IncludeArchive = editBmk.IncludeArchive,
                        CodePage = editBmk.CodePage,
                    };
                    string[] paths = editBmk.PathReferences.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    newBmk.FolderReferences.AddRange(paths);
                    BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                    BookmarkLibrary.Save();
                    Bookmarks.AddNewItem(editBmk);
                    SelectedBookmark = editBmk;
                }
            }
        }

        private void AddBookmark()
        {
            var editBmk = new BookmarkViewModel(new Bookmark());
            var dlg = new BookmarkDetailWindow
            {
                DataContext = editBmk,
                Owner = ownerWnd
            };

            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                var newBmk = new Bookmark(editBmk.SearchFor, editBmk.ReplaceWith, editBmk.FilePattern)
                {
                    Description = editBmk.Description,
                    IgnoreFilePattern = editBmk.IgnoreFilePattern,
                    TypeOfFileSearch = editBmk.TypeOfFileSearch,
                    TypeOfSearch = editBmk.TypeOfSearch,
                    CaseSensitive = editBmk.CaseSensitive,
                    WholeWord = editBmk.WholeWord,
                    Multiline = editBmk.Multiline,
                    Singleline = editBmk.Singleline,
                    BooleanOperators = editBmk.BooleanOperators,
                    IncludeSubfolders = editBmk.IncludeSubfolders,
                    MaxSubfolderDepth = editBmk.MaxSubfolderDepth,
                    IncludeHiddenFiles = editBmk.IncludeHidden,
                    IncludeBinaryFiles = editBmk.IncludeBinary,
                    UseGitignore = editBmk.UseGitignore,
                    IncludeArchive = editBmk.IncludeArchive,
                    CodePage = editBmk.CodePage,
                };
                string[] paths = editBmk.PathReferences.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                newBmk.FolderReferences.AddRange(paths);
                BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                BookmarkLibrary.Save();
                Bookmarks.AddNewItem(editBmk);
                SelectedBookmark = editBmk;
            }
        }
    }

    public class BookmarkViewModel : ViewModelBase
    {
        public static ObservableCollection<KeyValuePair<string, int>> Encodings { get; } = new ObservableCollection<KeyValuePair<string, int>>();

        public BookmarkViewModel(Bookmark bk)
        {
            IsEverythingAvailable = EverythingSearch.IsAvailable;
            IsGitInstalled = Utils.IsGitInstalled;

            Description = bk.Description;
            FilePattern = bk.FileNames;
            SearchFor = bk.SearchPattern;
            ReplaceWith = bk.ReplacePattern;

            HasExtendedProperties = bk.Version > 1;

            TypeOfSearch = bk.TypeOfSearch;
            CaseSensitive = bk.CaseSensitive;
            WholeWord = bk.WholeWord;
            Multiline = bk.Multiline;
            Singleline = bk.Singleline;
            BooleanOperators = bk.BooleanOperators;

            TypeOfFileSearch = bk.TypeOfFileSearch;
            IgnoreFilePattern = bk.IgnoreFilePattern;
            IncludeBinary = bk.IncludeBinaryFiles;
            IncludeHidden = bk.IncludeHiddenFiles;
            IncludeSubfolders = bk.IncludeSubfolders;
            MaxSubfolderDepth = bk.MaxSubfolderDepth;
            UseGitignore = bk.UseGitignore;
            IncludeArchive = bk.IncludeArchive;
            CodePage = bk.CodePage;
            PathReferences = string.Join(Environment.NewLine, bk.FolderReferences);

            UpdateTypeOfSearchState();
        }

        public BookmarkViewModel(BookmarkViewModel toCopy)
        {
            HasExtendedProperties = true;
            IsEverythingAvailable = EverythingSearch.IsAvailable;
            IsGitInstalled = Utils.IsGitInstalled;

            Description = toCopy.Description;
            FilePattern = toCopy.FilePattern;
            SearchFor = toCopy.SearchFor;
            ReplaceWith = toCopy.ReplaceWith;

            TypeOfSearch = toCopy.TypeOfSearch;
            TypeOfFileSearch = toCopy.TypeOfFileSearch;
            IgnoreFilePattern = toCopy.IgnoreFilePattern;

            IncludeBinary = toCopy.IncludeBinary;
            IncludeHidden = toCopy.IncludeHidden;
            IncludeSubfolders = toCopy.IncludeSubfolders;
            MaxSubfolderDepth = toCopy.MaxSubfolderDepth;
            UseGitignore = toCopy.UseGitignore;
            IncludeArchive = toCopy.IncludeArchive;
            CodePage = toCopy.CodePage;
            PathReferences = string.Join(Environment.NewLine, toCopy.PathReferences);

            CaseSensitive = toCopy.CaseSensitive;
            IsCaseSensitiveEnabled = toCopy.IsCaseSensitiveEnabled;

            WholeWord = toCopy.WholeWord;
            IsWholeWordEnabled = toCopy.IsWholeWordEnabled;

            Multiline = toCopy.Multiline;
            IsMultilineEnabled = toCopy.IsMultilineEnabled;

            Singleline = toCopy.Singleline;
            IsSinglelineEnabled = toCopy.IsSinglelineEnabled;

            BooleanOperators = toCopy.BooleanOperators;
            IsBooleanOperatorsEnabled = toCopy.IsBooleanOperatorsEnabled;
        }

        private void UpdateTypeOfSearchState()
        {
            IsCaseSensitiveEnabled = true;
            IsMultilineEnabled = true;
            IsSinglelineEnabled = true;
            IsWholeWordEnabled = true;
            IsBooleanOperatorsEnabled = true;

            if (TypeOfSearch == SearchType.XPath)
            {
                IsCaseSensitiveEnabled = false;
                IsMultilineEnabled = false;
                IsSinglelineEnabled = false;
                IsWholeWordEnabled = false;
                IsBooleanOperatorsEnabled = false;
                CaseSensitive = false;
                Multiline = false;
                Singleline = false;
                WholeWord = false;
                BooleanOperators = false;
            }
            else if (TypeOfSearch == SearchType.PlainText)
            {
                IsSinglelineEnabled = false;
                Singleline = false;
            }
            else if (TypeOfSearch == SearchType.Soundex)
            {
                IsCaseSensitiveEnabled = false;
                IsSinglelineEnabled = false;
                IsBooleanOperatorsEnabled = false;
                CaseSensitive = false;
                Singleline = false;
                BooleanOperators = false;
            }
        }


        private bool hasExtendedProperties = false;
        public bool HasExtendedProperties
        {
            get { return hasExtendedProperties; }
            set
            {
                if (hasExtendedProperties == value)
                    return;

                hasExtendedProperties = value;
                OnPropertyChanged("HasExtendedProperties");
            }
        }

        private string description = string.Empty;
        public string Description
        {
            get { return description; }
            set
            {
                if (description == value)
                    return;

                description = value;
                OnPropertyChanged("Description");
            }
        }

        private string filePattern = string.Empty;
        public string FilePattern
        {
            get { return filePattern; }
            set
            {
                if (filePattern == value)
                    return;

                filePattern = value;
                OnPropertyChanged("FilePattern");
            }
        }

        private string searchFor = string.Empty;
        public string SearchFor
        {
            get { return searchFor; }
            set
            {
                if (searchFor == value)
                    return;

                searchFor = value;
                OnPropertyChanged("SearchFor");
            }
        }

        private string replaceWith = string.Empty;
        public string ReplaceWith
        {
            get { return replaceWith; }
            set
            {
                if (replaceWith == value)
                    return;

                replaceWith = value;
                OnPropertyChanged("ReplaceWith");
            }
        }

        private string ignoreFilePattern = string.Empty;
        public string IgnoreFilePattern
        {
            get { return ignoreFilePattern; }
            set
            {
                if (ignoreFilePattern == value)
                    return;

                ignoreFilePattern = value;
                OnPropertyChanged("IgnoreFilePattern");
            }
        }

        private FileSearchType typeOfFileSearch = FileSearchType.Asterisk;
        public FileSearchType TypeOfFileSearch
        {
            get { return typeOfFileSearch; }
            set
            {
                if (typeOfFileSearch == value)
                    return;

                typeOfFileSearch = value;
                OnPropertyChanged("TypeOfFileSearch");
            }
        }

        private SearchType typeOfSearch = SearchType.PlainText;
        public SearchType TypeOfSearch
        {
            get { return typeOfSearch; }
            set
            {
                if (typeOfSearch == value)
                    return;

                typeOfSearch = value;
                OnPropertyChanged("TypeOfSearch");

                UpdateTypeOfSearchState();
            }
        }

        private bool caseSensitive = false;
        public bool CaseSensitive
        {
            get { return caseSensitive; }
            set
            {
                if (caseSensitive == value)
                    return;

                caseSensitive = value;
                OnPropertyChanged("CaseSensitive");
            }
        }

        private bool isCaseSensitiveEnabled = false;
        public bool IsCaseSensitiveEnabled
        {
            get { return isCaseSensitiveEnabled; }
            set
            {
                if (isCaseSensitiveEnabled == value)
                    return;

                isCaseSensitiveEnabled = value;
                OnPropertyChanged("IsCaseSensitiveEnabled");
            }
        }

        private bool wholeWord = false;
        public bool WholeWord
        {
            get { return wholeWord; }
            set
            {
                if (wholeWord == value)
                    return;

                wholeWord = value;
                OnPropertyChanged("WholeWord");
            }
        }

        private bool isWholeWordEnabled = false;
        public bool IsWholeWordEnabled
        {
            get { return isWholeWordEnabled; }
            set
            {
                if (isWholeWordEnabled == value)
                    return;

                isWholeWordEnabled = value;
                OnPropertyChanged("IsWholeWordEnabled");
            }
        }

        private bool multiline = false;
        public bool Multiline
        {
            get { return multiline; }
            set
            {
                if (multiline == value)
                    return;

                multiline = value;
                OnPropertyChanged("Multiline");
            }
        }

        private bool isMultilineEnabled = false;
        public bool IsMultilineEnabled
        {
            get { return isMultilineEnabled; }
            set
            {
                if (isMultilineEnabled == value)
                    return;

                isMultilineEnabled = value;
                OnPropertyChanged("IsMultilineEnabled");
            }
        }


        private bool singleline = false;
        public bool Singleline
        {
            get { return singleline; }
            set
            {
                if (singleline == value)
                    return;

                singleline = value;
                OnPropertyChanged("Singleline");
            }
        }

        private bool isSinglelineEnabled = false;
        public bool IsSinglelineEnabled
        {
            get { return isSinglelineEnabled; }
            set
            {
                if (isSinglelineEnabled == value)
                    return;

                isSinglelineEnabled = value;
                OnPropertyChanged("IsSinglelineEnabled");
            }
        }


        private bool booleanOperators = false;
        public bool BooleanOperators
        {
            get { return booleanOperators; }
            set
            {
                if (booleanOperators == value)
                    return;

                booleanOperators = value;
                OnPropertyChanged("BooleanOperators");
            }
        }

        private bool isbooleanOperatorsEnabled = false;
        public bool IsBooleanOperatorsEnabled
        {
            get { return isbooleanOperatorsEnabled; }
            set
            {
                if (isbooleanOperatorsEnabled == value)
                    return;

                isbooleanOperatorsEnabled = value;
                OnPropertyChanged("IsBooleanOperatorsEnabled");
            }
        }

        private bool includeSubfolders = true;
        public bool IncludeSubfolders
        {
            get { return includeSubfolders; }
            set
            {
                if (includeSubfolders == value)
                    return;

                includeSubfolders = value;
                OnPropertyChanged("IncludeSubfolders");

                if (!includeSubfolders)
                {
                    MaxSubfolderDepth = -1;
                }
            }
        }

        private int maxSubfolderDepth = -1;
        public int MaxSubfolderDepth
        {
            get { return maxSubfolderDepth; }
            set
            {
                if (value == maxSubfolderDepth)
                    return;

                maxSubfolderDepth = value;

                base.OnPropertyChanged(() => MaxSubfolderDepth);
            }
        }

        private bool includeHidden = false;
        public bool IncludeHidden
        {
            get { return includeHidden; }
            set
            {
                if (includeHidden == value)
                    return;

                includeHidden = value;
                OnPropertyChanged("IncludeHidden");
            }
        }

        private bool includeBinary = false;
        public bool IncludeBinary
        {
            get { return includeBinary; }
            set
            {
                if (includeBinary == value)
                    return;

                includeBinary = value;
                OnPropertyChanged("IncludeBinary");
            }
        }

        private bool useGitignore = false;
        public bool UseGitignore
        {
            get { return useGitignore; }
            set
            {
                if (useGitignore == value)
                    return;

                useGitignore = value;
                OnPropertyChanged("UseGitignore");
            }
        }

        private bool includeArchive = false;
        public bool IncludeArchive
        {
            get { return includeArchive; }
            set
            {
                if (includeArchive == value)
                    return;

                includeArchive = value;
                OnPropertyChanged("IncludeArchive");
            }
        }

        private int codePage = -1;
        public int CodePage
        {
            get { return codePage; }
            set
            {
                if (codePage == value)
                    return;

                codePage = value;
                OnPropertyChanged("CodePage");

                EncodingIndex = (Encodings.Select((kv, Index) => new { kv.Value, Index })
                    .FirstOrDefault(a => a.Value == codePage) ?? new { Value = 0, Index = 0 }).Index;
            }
        }

        private int encodingIndex = 0;
        public int EncodingIndex
        {
            get { return encodingIndex; }
            set
            {
                if (encodingIndex == value || encodingIndex < 0 || encodingIndex > Encodings.Count - 1)
                    return;

                encodingIndex = value;
                OnPropertyChanged("EncodingIndex");
            }
        }

        private string pathReferences = string.Empty;
        public string PathReferences
        {
            get { return pathReferences; }
            set
            {
                if (value == pathReferences)
                    return;

                pathReferences = value;
                OnPropertyChanged("PathReferences");
            }
        }

        private bool isEverythingAvailable;
        public bool IsEverythingAvailable
        {
            get { return isEverythingAvailable; }
            set
            {
                if (value == isEverythingAvailable)
                    return;

                isEverythingAvailable = value;

                base.OnPropertyChanged("IsEverythingAvailable");
            }
        }

        private bool isGitInstalled;
        public bool IsGitInstalled
        {
            get { return isGitInstalled; }
            set
            {
                if (value == isGitInstalled)
                    return;

                isGitInstalled = value;

                base.OnPropertyChanged("IsGitInstalled");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is BookmarkViewModel other)
            {
                return FilePattern == other.FilePattern &&
                    SearchFor == other.SearchFor &&
                    ReplaceWith == other.ReplaceWith;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ FilePattern.GetHashCode();
                hashCode = (hashCode * 397) ^ SearchFor.GetHashCode();
                hashCode = (hashCode * 397) ^ ReplaceWith.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{SearchFor} to {ReplaceWith} on {FilePattern} :: {Description}";
        }
    }
}
