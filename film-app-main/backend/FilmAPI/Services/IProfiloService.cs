using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IProfiloService
{
    Task<UserInfoDTO?> GetProfiloAsync(int userId);
    Task<UserInfoDTO?> UpdateProfiloAsync(int userId, ProfiloUpdateDTO dto);
    Task<CinemaPreferitoDTO?> GetCinemaPreferitoAsync(int userId);
    Task<CinemaPreferitoDTO> SetCinemaPreferitoAsync(int userId, int? cinemaId);
    Task<FilmPreferitoDTO?> GetFilmPreferitoAsync(int userId);
    Task<FilmPreferitoDTO> SetFilmPreferitoAsync(int userId, int? filmId);
    Task<bool> DeleteAccountAsync(int userId);
    Task<AccountExportDTO?> ExportAccountDataAsync(int userId);
}
