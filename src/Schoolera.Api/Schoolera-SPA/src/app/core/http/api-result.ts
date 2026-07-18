export interface ApiResult<T> {
  succeeded: boolean;
  data: T | null;
  errors: string[];
  /** Stable codes for client branching. Do not parse `errors` text. */
  errorCodes?: string[];
}
