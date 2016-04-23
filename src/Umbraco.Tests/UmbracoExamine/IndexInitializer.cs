﻿using System;
using System.Linq;
using Examine;
using Examine.LuceneEngine.Config;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Store;
using Moq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Services;
using Umbraco.Core.Strings;
using UmbracoExamine;
using IContentService = Umbraco.Core.Services.IContentService;
using IMediaService = Umbraco.Core.Services.IMediaService;
using Version = Lucene.Net.Util.Version;

namespace Umbraco.Tests.UmbracoExamine
{
	/// <summary>
	/// Used internally by test classes to initialize a new index from the template
	/// </summary>
	internal static class IndexInitializer
	{
		public static UmbracoContentIndexer GetUmbracoIndexer(
            ProfilingLogger profilingLogger,
            Directory luceneDir, 
            Analyzer analyzer = null,
            IContentService contentService = null,
            IMediaService mediaService = null,
            IDataTypeService dataTypeService = null,
            IMemberService memberService = null,
            IUserService userService = null)
		{            
		    if (contentService == null)
		    {
                contentService = Mock.Of<IContentService>();
		    }
		    if (userService == null)
		    {
		        userService = Mock.Of<IUserService>(x => x.GetProfileById(It.IsAny<int>()) == Mock.Of<IProfile>(p => p.Id == (object)0 && p.Name == "admin"));
		    }
            if (mediaService == null)
            {
                long totalRecs;

                var demoData = new ExamineDemoDataMediaService();

                var allRecs = demoData.GetLatestMediaByXpath("//node")
                    .Root
                    .Elements()
                    .Select(x => Mock.Of<IMedia>(
                        m =>
                            m.Id == (int) x.Attribute("id") &&
                            m.ParentId == (int) x.Attribute("parentID") &&
                            m.Level == (int) x.Attribute("level") &&
                            m.CreatorId == 0 &&
                            m.SortOrder == (int) x.Attribute("sortOrder") &&
                            m.CreateDate == (DateTime) x.Attribute("createDate") &&
                            m.UpdateDate == (DateTime) x.Attribute("updateDate") &&
                            m.Name == (string) x.Attribute("nodeName") &&
                            m.Path == (string) x.Attribute("path") &&
                            m.Properties == new PropertyCollection() &&
                            m.ContentType == Mock.Of<IMediaType>(mt =>
                                mt.Alias == (string) x.Attribute("nodeTypeAlias") &&
                                mt.Id == (int) x.Attribute("nodeType"))))
                    .ToArray();
                    

                mediaService = Mock.Of<IMediaService>(
                    x => x.GetPagedDescendants(
                        It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), out totalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<string>()) 
                        ==
                        allRecs);
            }
            if (dataTypeService == null)
            {
                dataTypeService = Mock.Of<IDataTypeService>();
            }        

            if (analyzer == null)
            {
                analyzer = new StandardAnalyzer(Version.LUCENE_30);
            }

		    //var indexSet = new IndexSet();
      //      var indexCriteria = indexSet.ToIndexCriteria(dataService, UmbracoContentIndexer.IndexFieldPolicies);
            
		    //var i = new UmbracoContentIndexer(indexCriteria,
		    //                                  luceneDir, //custom lucene directory
      //                                        dataService,
      //                                        contentService,
      //                                        mediaService,
      //                                        dataTypeService,
      //                                        userService,
      //                                        new[] { new DefaultUrlSegmentProvider() },
		    //                                  analyzer,
		    //                                  false);

			//i.IndexSecondsInterval = 1;

            var options = new UmbracoContentIndexerOptions(false, false);

		    var i = new UmbracoContentIndexer(
		        new[]
		        {
		            new FieldDefinition("", FieldDefinitionTypes.FullText)
		        },
		        luceneDir,
                analyzer,
                profilingLogger,
                contentService,
                mediaService,
                dataTypeService,
                userService,
                new[] { new DefaultUrlSegmentProvider() },
                new UmbracoContentValueSetValidator(options, Mock.Of<IPublicAccessService>()),
                options);

			i.IndexingError += IndexingError;

			return i;
		}
        public static UmbracoExamineSearcher GetUmbracoSearcher(Directory luceneDir, Analyzer analyzer = null)
		{
            if (analyzer == null)
            {
                analyzer = new StandardAnalyzer(Version.LUCENE_29);
            }
            return new UmbracoExamineSearcher(luceneDir, analyzer);
		}
		
		public static LuceneSearcher GetLuceneSearcher(Directory luceneDir)
		{
			return new LuceneSearcher(luceneDir, new StandardAnalyzer(Version.LUCENE_29));
		}
		
		public static MultiIndexSearcher GetMultiSearcher(Directory pdfDir, Directory simpleDir, Directory conventionDir, Directory cwsDir)
		{
			var i = new MultiIndexSearcher(new[] { pdfDir, simpleDir, conventionDir, cwsDir }, new StandardAnalyzer(Version.LUCENE_29));
			return i;
		}


		internal static void IndexingError(object sender, IndexingErrorEventArgs e)
		{
			throw new ApplicationException(e.Message, e.Exception);
		}


	}
}