import type {
  FavoriteSchoolListItemDto,
  FavoriteSchoolListItemDtoPagedResult,
  FavoriteSchoolUnavailableDto,
  PublicSchoolListItemDto,
} from '../../../core/api-client/SwaggerClient.service';
import type { ApiResult } from '../../../core/http/api-result';

/** Re-exports for Parent favorites UI (NSwag DTOs). */
export type FavoriteSchoolListItem = FavoriteSchoolListItemDto;
export type FavoriteSchoolUnavailable = FavoriteSchoolUnavailableDto;
export type FavoriteSchoolCard = PublicSchoolListItemDto;
export type FavoriteSchoolPagedResult = FavoriteSchoolListItemDtoPagedResult;
export type FavoriteSchoolListResult = ApiResult<FavoriteSchoolPagedResult>;
export type FavoriteBoolResult = ApiResult<boolean>;
