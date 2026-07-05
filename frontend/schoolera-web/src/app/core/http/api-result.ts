export interface ApiResult<T> {
  succeeded: boolean;
  data: T | null;
  errors: string[];
}
