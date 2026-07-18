export interface HasUnsavedPortalChanges {
  hasUnsavedChanges(): boolean;
}

export function componentHasUnsavedChanges(
  component: unknown,
): component is HasUnsavedPortalChanges {
  return (
    typeof component === 'object' &&
    component !== null &&
    'hasUnsavedChanges' in component &&
    typeof (component as HasUnsavedPortalChanges).hasUnsavedChanges === 'function'
  );
}
