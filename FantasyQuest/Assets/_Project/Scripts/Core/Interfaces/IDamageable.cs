namespace Project.Core.Interfaces
{
    /// <summary>
    /// ダメージを受け取ることができる全オブジェクト共通のインターフェース。
    /// これを実装していれば、プレイヤー、敵、木箱など対象に関わらずHitboxからダメージを与えられます。
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
