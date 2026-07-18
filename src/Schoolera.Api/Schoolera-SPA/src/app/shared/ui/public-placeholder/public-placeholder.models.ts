export interface PublicPlaceholderActionKeys {
  readonly labelKey: string;
  readonly route: string;
}

export interface PublicPlaceholderContentKeys {
  readonly titleKey: string;
  readonly descriptionKey: string;
  readonly breadcrumbKeys?: ReadonlyArray<{ labelKey: string; route?: string }>;
  readonly primaryAction?: PublicPlaceholderActionKeys;
  readonly secondaryAction?: PublicPlaceholderActionKeys;
}
