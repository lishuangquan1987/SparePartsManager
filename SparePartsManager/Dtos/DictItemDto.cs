namespace SparePartsManager.Dtos;

/// <summary>
/// 字典项数据传输对象（Id + Name），用于 ComboBox 的
/// SelectedValuePath="Id" / DisplayMemberPath="Name" 绑定。
/// </summary>
public class DictItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
