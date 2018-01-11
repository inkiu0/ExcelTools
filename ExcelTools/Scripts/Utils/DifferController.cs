using System.IO;
using System.Collections.Generic;
using System.Linq;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.Collections.ObjectModel;
using ExcelTools.Scripts.UI;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace ExcelTools.Scripts.Utils
{
    class DifferController
    {
        //本地修改的文件路径
        private string _localPath;
        public string LocalPath
        {
            get { return _localPath; }
        }
        //
        private string _tempPath;
        public string TempPath
        {
            get { return _tempPath; }
        }
        

        //存储TempPath文件中被删除的行号
        private List<int> _deletedList = new List<int>();
        //存储localExcelPath文件中被添加的行号
        private List<int> _addedList = new List<int>();
        //存储TempPath文件中需要插入的行号
        private List<int> _addedToList = new List<int>();
        //存储deletedList和addedList中的公共值
        private List<int> _modifiedList = new List<int>();


        private List<int> _cancelList = new List<int>();

        #region 以下为UI绑定数据及设置
        public string SelectedText;
        private ObservableCollection<DiffItem> _diffItems;
        public ObservableCollection<DiffItem> DiffItems
        {
            get
            {
                if (_diffItems == null)
                {
                    _diffItems = new ObservableCollection<DiffItem>();
                    _diffItems.CollectionChanged += (sender, e) =>
                    {
                        if (e.OldItems != null)
                        {
                            foreach (DiffItem diffItems in e.OldItems)
                            {
                                diffItems.PropertyChanged -= ItemPropertyChanged;
                            }
                        }

                        if (e.NewItems != null)
                        {
                            foreach (DiffItem diffItems in e.NewItems)
                            {
                                diffItems.PropertyChanged += ItemPropertyChanged;
                            }
                        }
                    };
                }
                return _diffItems;
            }
        }
        #endregion

        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                if (sender is DiffItem diffItem)
                {
                    //刷新UI
                    GenSelectedText();
                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    mainWindow.diffChooseBox.Text = SelectedText;
                    //修改逻辑数据
                    if (diffItem.IsChecked == false)
                        _cancelList.Add(diffItem.Row);
                    else
                        _cancelList.Remove(diffItem.Row);
                }
            }
        }

        private void GenSelectedText()
        {
            IEnumerable<DiffItem> diffItems = DiffItems.Where(b => b.IsChecked == true);

            StringBuilder builder = new StringBuilder();

            foreach (DiffItem item in diffItems)
            {
                builder.Append(item.Row + ";");
            }

            SelectedText = builder == null ? string.Empty : builder.ToString();
        }


        public DifferController(string localExcelPath, string tempPath)
        {
            _localPath = localExcelPath;
            _tempPath = tempPath;
        }

        public bool Differ()
        {
            Excel localExcel = Excel.Parse(_localPath, false);
            Excel tempExcel = Excel.Parse(_tempPath, false);
            string localExcelTmp = localExcel.ToString();
            string tempTmp = tempExcel.ToString();
            if (localExcelTmp == tempTmp)
            {
                return false;
            }
            else
            {
                _deletedList.Clear();
                _addedList.Clear();
                _addedToList.Clear();
                _modifiedList.Clear();
                for (int i = 0; i < tempExcel.rows.Count; i++)
                {
                    for (int j = 0; j < localExcel.rows.Count; j++)
                    {
                        if (tempExcel.rows[i].ToStringWithOutIndex() == localExcel.rows[j].ToStringWithOutIndex())
                        {
                            break;
                        }
                        if (j == localExcel.rows.Count - 1)
                        {
                            _deletedList.Add(i + 5);
                        }
                    }
                }
                for (int i = 0; i < localExcel.rows.Count; i++)
                {
                    for (int j = 0; j < tempExcel.rows.Count; j++)
                    {
                        if (tempExcel.rows[j].ToStringWithOutIndex() == localExcel.rows[i].ToStringWithOutIndex())
                        {
                            break;
                        }
                        if (j == tempExcel.rows.Count - 1)
                        {
                            _addedList.Add(i + 5);
                            _addedToList.Add(i + 5);
                        }
                    }
                }
                IEnumerable<int> en = _deletedList.Intersect(_addedList);
                foreach (int index in en)
                {
                    _modifiedList.Add(index);
                }
                RefreshUIData();
                return true;
            }
        }

        //刷新UI绑定的数据
        private void RefreshUIData()
        {
            DiffItems.Clear();
            for (int i = 0; i < _modifiedList.Count; i++)
            {
                DiffItems.Add(new DiffItem()
                {
                    Row = _addedList[i],
                    State = "modified",
                    IsChecked = true,
                    Context = ""
                });
            }
            for (int i = 0; i < _addedList.Count; i++)
            {
                if (!_modifiedList.Contains(_addedList[i]))
                {
                    DiffItems.Add(new DiffItem()
                    {
                        Row = _addedList[i],
                        State = "added",
                        IsChecked = true,
                        Context = ""
                    });
                }
            }
            for (int i = 0; i < _deletedList.Count; i++)
            {
                if (!_modifiedList.Contains(_deletedList[i]))
                {
                    DiffItems.Add(new DiffItem()
                    {
                        Row = _deletedList[i],
                        State = "deleted",
                        IsChecked = true,
                        Context = ""
                    });
                }
            }
            GenSelectedText();
        }

        public void ConfirmChangesAndCommit()
        {
            CancelChanges(_cancelList);
            ModifyTempFile();
            //TODO:生成并提交
            //
            //
        }


        //取消个别改动时用这个，全部取消直接Revert
        private void CancelChanges(List<int> rowsExclusion)
        {
            for (int i = 0; i < rowsExclusion.Count; i++)
            {
                if (_modifiedList.IndexOf(rowsExclusion[i]) != -1)
                {
                    _modifiedList.Remove(rowsExclusion[i]);
                    _deletedList.Remove(rowsExclusion[i]);
                    int index = _addedList.IndexOf(rowsExclusion[i]);
                    _addedList.RemoveAt(index);
                    _addedToList.RemoveAt(index);                    
                }
                else if (_deletedList.IndexOf(rowsExclusion[i]) != -1)
                {
                    for (int j = 0; j < _addedList.Count; j++)
                    {
                        if (_addedList[j] > _deletedList[i])
                        {
                            _addedToList[j]++;
                        }
                    }
                    _deletedList.RemoveAt(_deletedList.IndexOf(rowsExclusion[i]));
                }
                else if (_addedList.IndexOf(rowsExclusion[i]) != -1)
                {
                    int index = _addedList.IndexOf(rowsExclusion[i]);
                    for (int j = _addedList.IndexOf(rowsExclusion[i]) + 1; j < _addedList.Count; j++)
                    {
                        _addedToList[j]--;
                    }
                    _addedList.RemoveAt(index);
                    _addedToList.RemoveAt(index);
                }
            }
        }

        private void ModifyTempFile()
        {
            XSSFWorkbook tmpWk = null;
            XSSFWorkbook locWk = null;
            using (FileStream tmpFs = File.Open(_tempPath, FileMode.Open, FileAccess.ReadWrite))
            {
                tmpWk = new XSSFWorkbook(tmpFs);
                tmpFs.Close();
            }
            using (FileStream locFs = File.Open(_localPath, FileMode.Open, FileAccess.Read))
            {
                locWk = new XSSFWorkbook(locFs);
                locFs.Close();
            }
            ISheet tmpSheet = tmpWk.GetSheetAt(0);
            ISheet locSheet = locWk.GetSheetAt(0);

            //修改行
            //for (int i = 0; i < modifiedList.Count; i++)
            //{
            //    IRow tmpRow = tmpSheet.GetRow(modifiedList[i] - 1);
            //    IRow locRow = locSheet.GetRow(modifiedList[i] - 1);
            //    for (int j = 0; j < tmpRow.Cells.Count; j++)
            //    {
            //        ICell cell = tmpRow.GetCell(j);
            //        //cell.SetCellValue("");
            //        tmpRow.RemoveCell(cell);
            //    }
            //    for (int j = 0; j < locRow.Cells.Count; j++)
            //    {
            //        ICell cell = tmpRow.CreateCell(j);
            //        //ICell cell = tmpRow.GetCell(j);
            //        if (locRow.GetCell(j).CellType == CellType.Numeric)
            //            cell.SetCellValue(locRow.GetCell(j).NumericCellValue);
            //        else if (locRow.GetCell(j).CellType == CellType.String)
            //            cell.SetCellValue(locRow.GetCell(j).StringCellValue);
            //    }
            //}

            //删除行
            for (int i = 0; i < _deletedList.Count; i++)
            {
                IRow tmpRow = tmpSheet.GetRow(_deletedList[i] - 1);
                tmpSheet.RemoveRow(tmpRow);
            }
            //紧凑
            for (int i = 0; i <= tmpSheet.LastRowNum; i++)
            {
                if (tmpSheet.GetRow(i) == null)
                {
                    tmpSheet.ShiftRows(i + 1, tmpSheet.LastRowNum, -1, true, true);
                    i--;
                }
            }
            //插入行
            for (int i = 0; i < _addedList.Count; i++)
            {
                if (_addedList[i] - 1 <= tmpSheet.LastRowNum)
                {
                    tmpSheet.ShiftRows(_addedToList[i] - 1, tmpSheet.LastRowNum, 1, true, true);
                }
                IRow tmpRow = tmpSheet.CreateRow(_addedToList[i] - 1);
                IRow locRow = locSheet.GetRow(_addedList[i] - 1);
                for (int j = 0; j < locRow.LastCellNum; j++)
                {
                    ICell tmpCell = tmpRow.CreateCell(j);
                    ICell locCell = locRow.GetCell(j);
                    if (locCell != null)
                    {
                        ICellStyle cellStyle = tmpWk.CreateCellStyle();
                        cellStyle.CloneStyleFrom(locCell.CellStyle);
                        tmpCell.CellStyle = cellStyle;
                        if (locCell.CellType == CellType.Numeric)
                            tmpCell.SetCellValue(locCell.NumericCellValue);
                        else if (locCell.CellType == CellType.String)
                            tmpCell.SetCellValue(locCell.StringCellValue);
                        else if (locCell.CellType == CellType.Blank)
                            tmpCell.SetCellValue(locCell.StringCellValue);
                    }
                }
            }

            FileUtil.SetHidden(_tempPath, false);
            using (FileStream tmpFs = File.Create(_tempPath))
            {
                tmpWk.Write(tmpFs);
                tmpFs.Close();
            }
            FileUtil.SetHidden(_tempPath, true);
        }
    }
}
