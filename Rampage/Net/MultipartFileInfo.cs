using System;
namespace Rampage.Net
{
    /// <summary>
    /// multipart 上传文件信息：表单字段名、上传文件名与文件字节内容
    /// Multipart upload file info: the form field name, uploaded file name and file byte content
    /// </summary>
    public class MultipartFileInfo
    {
        /// <summary>表单字段名，缺省为 "UploadFile" / The form field name, "UploadFile" when absent</summary>
        public string? Name { get; set; }

        /// <summary>上传文件名，缺省为 "UploadFile" / The uploaded file name, "UploadFile" when absent</summary>
        public string? FileName { get; set; }

        /// <summary>文件字节内容 / The file byte content</summary>
        public byte[]? Data { get; set; }

    }
}
