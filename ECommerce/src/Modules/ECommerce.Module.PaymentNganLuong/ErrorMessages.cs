using System.Collections.Generic;

namespace ECommerce.Module.PaymentNganLuong
{
    public static class ErrorMessages
    {
        private static IDictionary<string, string> errors = new Dictionary<string, string>()
        {
            { "00", "Không có l?i" },
            { "99", "L?i không du?c d?nh nghia ho?c không rõ nguyên nhân" },
            { "02", "Ð?a ch? IP c?a merchant g?i t?i NganLuong.vn không du?c ch?p nh?n" },
            { "03", "Sai tham s? g?i t?i NganLuong.vn (có tham s? sai tên ho?c ki?u d? li?u)" },
            { "04", "Tên hàm API do merchant g?i t?i không h?p l? (không t?n t?i)" },
            { "05", "Sai version c?a API" },
            { "06", "Mã merchant không t?n t?i ho?c chua du?c kích ho?t" },
            { "07", "Sai m?t kh?u c?a merchant" },
            { "08", "Tài kho?n ngu?i bán hàng không t?n t?i" },
            { "09", "Tài kho?n ngu?i nh?n ti?n dang b? phong t?a" },
            { "10", "Hóa don thanh toán không h?p l?" },
            { "11", "S? ti?n thanh toán không h?p l?" },
            { "12", "Ðon v? ti?n t? không h?p l?" },
            { "29", "Token không t?n t?i" },
            { "80", "Không thêm du?c don hàng" },
            { "81", "Ðon hàng chua du?c thanh toán" },
            { "110", "Ð?a ch? email tài kho?n nh?n ti?n không ph?i email chính" },
            { "111", "Tài kho?n nh?n ti?n dang b? khóa" },
            { "113", "Tài kho?n nh?n ti?n chua c?u hình là ngu?i bán n?i dung s?" },
            { "114", "Giao d?ch dang th?c hi?n, chua k?t thúc" },
            { "115", "Giao d?ch b? h?y" },
            { "118", "tax_amount không h?p l?" },
            { "119", "discount_amount không h?p l?" },
            { "120", "fee_shipping không h?p l?" },
            { "121", "return_url không h?p l?" },
            { "122", "cancel_url không h?p l?" },
            { "123", "items không h?p l?" },
            { "124", "transaction_info không h?p l?" },
            { "125", "quantity không h?p l?" },
            { "126", "order_description không h?p l?" },
            { "127", "affiliate_code không h?p l?" },
            { "128", "time_limit không h?p l?" },
            { "129", "buyer_fullname không h?p l?" },
            { "130", "buyer_email không h?p l?" },
            { "131", "buyer_mobile không h?p l?" },
            { "132", "buyer_address không h?p l?" },
            { "133", "total_item không h?p l?" },
            { "134", "payment_method, bank_code không h?p l?" },
            { "135", "L?i k?t n?i t?i h? th?ng ngân hàng" },
            { "140", "Ðon hàng không h? tr? thanh toán tr? góp" }
        };

        public static string GetMessage(string errorCode)
        {
            if(errors.ContainsKey(errorCode))
            {
                return $"{errorCode} - {errors[errorCode]}";
            }

            return $"{errorCode} - L?i không xác d?nh";
        }
    }
}
