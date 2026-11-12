// C# API의 Result<T>와 같은 모양
export type ApiResult<T> = {
  success: boolean;
  code: string;
  message: string;
  data: T | null;
};

// C# API의 UserInfoModel과 같은 모양
export type UserInfo = {
  userId: number;
  email: string;
  penName: string;
};

// /api/... 를 호출하고, 응답을 항상 ApiResult 모양으로 돌려준다
export async function apiFetch<T>(
  path: string,
  options: { method?: "GET" | "POST" | "PATCH" | "DELETE"; body?: unknown } = {},
): Promise<ApiResult<T>> {
  try {
    const response = await fetch(path, {
      method: options.method ?? "GET",
      headers: options.body ? { "Content-Type": "application/json" } : undefined,
      body: options.body ? JSON.stringify(options.body) : undefined,
    });

    // 401처럼 본문이 비어 있는 응답도 있어서, 내용이 있을 때만 JSON으로 읽는다
    const text = await response.text();
    if (text) return JSON.parse(text) as ApiResult<T>;

    return {
      success: response.ok,
      code: response.status === 401 ? "Unauthorized" : String(response.status),
      message: response.status === 401 ? "로그인이 필요합니다." : "요청을 처리하지 못했습니다.",
      data: null,
    };
  } catch {
    return { success: false, code: "NetworkError", message: "서버에 연결할 수 없습니다.", data: null };
  }
}
