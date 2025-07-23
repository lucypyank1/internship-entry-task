using System.ComponentModel.DataAnnotations;


namespace krestiki_noliki_api.DTOs
{

    public class GameCreateDto
    {
        [Range(3, 10, ErrorMessage = "Размер доски должен быть от 3 до 10")]
        public int? BoardSize { get; set; }

        [Range(3, 10, ErrorMessage = "Длина для победы должна быть от 3 до 10")]
        public int? WinLength { get; set; }
    }


}
