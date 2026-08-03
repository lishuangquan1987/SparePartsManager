namespace SparePartsManager.Models;

/// <summary>
/// 下拉框绑定的字典项（Id + Name），用于 ComboBox 的
/// SelectedValuePath="Id" / DisplayMemberPath="Name" 绑定。
/// </summary>
public class DictItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
