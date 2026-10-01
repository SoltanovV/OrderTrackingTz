export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message);
  }
}
export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    const validation = problem.errors ? Object.values(problem.errors).flat().join(' ') : '';
    throw new ApiError(
      validation || problem.title || 'Не удалось загрузить данные. Попробуйте ещё раз.',
      response.status,
    );
  }
  return response.json() as Promise<T>;
}
