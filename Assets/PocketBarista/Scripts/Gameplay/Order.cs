public enum OrderState
{
    Free,        // Havuzda
    Pending,     // İstasyon bekliyor
    Preparing,   // İstasyonda hazırlanıyor
    Ready,       // Tezgahta, servis bekliyor
    Delivered,   // Müşteride, tüketiliyor
    Cancelled
}

/// <summary>
/// Havuzlanan sipariş nesnesi. Version her yeniden kullanımda artar: eski referans tutan
/// personel/istasyon "Version" karşılaştırarak siparişin hâlâ aynı olup olmadığını anlar.
/// </summary>
public sealed class Order
{
    public int Version { get; private set; }
    public OrderState State;
    public ProductSO Product;
    public CustomerAI Customer;
    public Table Table;
    public Station Station;
    public StaffAI Courier;

    internal void Init(ProductSO product, CustomerAI customer, Table table)
    {
        Version++;
        State = OrderState.Pending;
        Product = product;
        Customer = customer;
        Table = table;
        Station = null;
        Courier = null;
    }

    internal void Clear()
    {
        Version++;
        State = OrderState.Free;
        Product = null;
        Customer = null;
        Table = null;
        Station = null;
        Courier = null;
    }
}
