using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 点选验证码基类（模板方法：由子类实现 CreateImage 产出 PNG 图片与坐标范围验证结果） /
    /// Base class for click captchas (template method: subclasses implement CreateImage to produce PNG bytes and the region-based result).
    /// </summary>
    /// <remarks>
    ///  Description:    点选验证码基类
    ///                  与 VerificationCodeBase（字符串答案契约）平行的点选式契约：答案为各目标字的
    ///                  可点击坐标范围数组，前端按序提交点击坐标点，由 ClickCodeResult.Verify 完成比对。
    ///                  本类型不进入 VerificationCodeManager.AllCodeTypes 随机池（该池契约为字符串）。
    ///  Description:    A click-style contract parallel to VerificationCodeBase (whose answer is a string):
    ///  the answer is the array of clickable regions of the target characters; the frontend submits click
    ///  points in order and ClickCodeResult.Verify does the comparison. Types here never join the
    ///  VerificationCodeManager.AllCodeTypes random pool (whose contract is a string).
    ///
    /// </remarks>
    public abstract class ClickVerificationCodeBase
    {
        /// <summary>
        /// 初始化点选验证码基类 / Initializes the base click captcha class.
        /// </summary>
        public ClickVerificationCodeBase()
        {
        }

        /// <summary>
        /// 获取点选验证码类型名（返回具体实现类的类名） /
        /// Gets the type name of the concrete implementation.
        /// </summary>
        public string Type
        {
            get { return this.GetType().Name; }
        }

        /// <summary>
        /// 获取点选验证码样式的中文名称（用于展示） / Gets the display name of the click captcha style.
        /// </summary>
        public abstract string Name
        {
            get;
        }

        /// <summary>
        /// 获取提示文案（默认“请依次点击图片中的文字”）。提示条已绘制在图片内，此属性供页面输入位等 UI 兜底展示 /
        /// Gets the hint text (default "请依次点击图片中的文字"). The prompt bar is drawn into the image itself;
        /// this property backs any page-side UI that still needs a textual hint.
        /// </summary>
        public virtual string Tip
        {
            get { return "请依次点击图片中的文字"; }
        }

        /// <summary>
        /// 生成点选验证码图片 /
        /// Generates the click captcha image.
        /// </summary>
        /// <param name="result">输出：验证结果（目标字串 + 各目标字的可点击坐标范围，按提示顺序） /
        /// Output: the verification result (the target code plus each target's clickable region, in prompt order).</param>
        /// <returns>PNG 图片字节数组（精确长度，无冗余字节） / The PNG image bytes (exact length, no trailing slack).</returns>
        public abstract byte[] CreateImage(out ClickCodeResult result);

        /// <summary>
        /// 获取是否禁用输入法：点选验证码全程鼠标点击，无需输入，默认 false 与中文验证码一致 /
        /// Whether the IME should be disabled: clicks only, no typing involved; false by default, matching the Chinese styles.
        /// </summary>
        public virtual bool DisableIme
        {
            get
            {
                return false;
            }
        }
    }
}
