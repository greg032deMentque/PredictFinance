export class PaginateInterface<T> {
  constructor(
    public Items: T[] = [],
    public Total: number = 0,
    public Page: number = 0,
    public PageSize: number = 0

  ) { }
}


