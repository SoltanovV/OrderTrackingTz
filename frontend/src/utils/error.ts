export const errorMessage = (error: unknown) =>
  error instanceof Error ? error.message : 'Произошла ошибка.';
