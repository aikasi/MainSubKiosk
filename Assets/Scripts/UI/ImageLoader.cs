using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 파일 경로로부터 Texture2D를 비동기로 로딩하고,
/// UI.Image에 사용할 Sprite를 생성합니다.
/// </summary>
public class ImageLoader : MonoBehaviour
{

    /// <summary>
    /// 파일 경로에서 비동기로 Texture2D를 로드합니다.
    /// 메인 스레드 블로킹을 최소화하기 위해 async/await를 사용합니다.
    /// </summary>
    /// <param name="filePath">이미지 파일의 절대 경로</param>
    /// <returns>성공 시 Texture2D, 실패 시 null</returns>
    public async Task<Texture2D> LoadTextureAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            LogError("표시할 이미지의 파일 경로가 주어지지 않았습니다.");
            return null;
        }

        if (!File.Exists(filePath))
        {
            LogError($"다음 경로에서 이미지 파일을 찾을 수 없습니다: {filePath}");
            return null;
        }

        byte[] fileData;
        try
        {
            // 백그라운드 스레드에서 파일 읽기
            fileData = await Task.Run(() => File.ReadAllBytes(filePath));
        }
        catch (System.Exception ex)
        {
            LogError($"윈도우에서 이미지 파일을 읽는데 실패했습니다: {filePath} — {ex.Message}");
            return null;
        }

        if (fileData == null || fileData.Length == 0)
        {
            LogError($"이미지 파일에 내용이 없습니다 (크기 0바이트): {filePath}");
            return null;
        }

        // Texture2D 생성은 메인 스레드에서 수행해야 함
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        if (!texture.LoadImage(fileData))
        {
            LogError($"이미지 파일을 화면에 띄울 수 없습니다 (파일 손상 가능성): {filePath}");
            Object.Destroy(texture);
            return null;
        }

        return texture;
    }

    /// <summary>
    /// Texture2D를 UI.Image에서 사용할 수 있는 Sprite로 변환합니다.
    /// </summary>
    /// <param name="texture">원본 Texture2D</param>
    /// <returns>생성된 Sprite, 실패 시 null</returns>
    public Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null)
        {
            LogError("원본 그림을 유니티 화면용(Sprite)으로 변환하는 데 실패했습니다.");
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    /// <summary>
    /// 에러 로그를 기록합니다.
    /// </summary>
    private void LogError(string message)
    {
        string fullMsg = $"[ERROR] ImageLoader: {message}";
        Debug.LogError(fullMsg);

    }
}
